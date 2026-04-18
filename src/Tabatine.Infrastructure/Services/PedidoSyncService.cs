using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tabatine.Omie.Client.Models.Pedidos;
using Tabatine.Infrastructure.Repositories;
using Tabatine.Omie.Client.Models;


namespace Tabatine.Infrastructure.Services
{
    public class PedidoSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState, 
        ILogger<PedidoSyncService> logger,
        IDistributedLockService lockService) : ISyncService
    {
        private const string EntityName = "pedido_venda";

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Pedidos de Venda...");
            
            var lastSyncDate = await syncState.GetLastSyncDateAsync("Pedidos", ct);
            var syncStartTime = DateTime.UtcNow;

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarPedidosAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);
                
                if (response == null || response.PedidosVenda == null || response.PedidosVenda.Count == 0) break;

                await ProcessPedidosBatchAsync(response.PedidosVenda, ct);
                
                logger.LogInformation("Página {Pagina} de {Total} de Pedidos sincronizada.", pagina, response.TotalDePaginas);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("Pedidos", syncStartTime, ct);
            }
            logger.LogInformation("Sincronização de Pedidos finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            // Sync individual (webhook): aguarda até 30s para garantir que a notificação seja processada
            var lockToken = await AcquireResourceLockAsync(omieId, ct, wait: true);
            if (lockToken == null)
            {
                logger.LogWarning("Não foi possível adquirir trava para o Pedido {OmieId} após espera. Abortando sync individual.", omieId);
                return;
            }

            try 
            {
                logger.LogInformation("Sincronizando Pedido específico OmieId: {OmieId}", omieId);
                var omiePedido = await omieClient.ConsultarPedidoAsync(omieId, ct);
                
                if (omiePedido != null)
                {
                    await ProcessPedidosBatchAsync(new List<OmiePedido> { omiePedido }, ct, 
                        preAcquiredLocks: new Dictionary<long, string> { { omieId, lockToken } });
                }
                else
                {
                    logger.LogWarning("Pedido OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
                }
            }
            finally 
            {
                await lockService.ReleaseLockAsync(GetLockKey(omieId), lockToken, ct);
            }
        }

        private async Task<string?> AcquireResourceLockAsync(long omieId, CancellationToken ct, bool wait = true)
        {
            var lockKey = GetLockKey(omieId);
            var lockToken = Guid.NewGuid().ToString();
            
            if (await lockService.TryAcquireLockAsync(lockKey, lockToken, TimeSpan.FromMinutes(2), ct))
                return lockToken;

            if (!wait) return null;

            // SyncByIdAsync: aguarda até 30s (60 × 500ms) para garantir processamento de webhooks
            for (int i = 1; i < 60; i++) 
            {
                await Task.Delay(500, ct); 
                if (await lockService.TryAcquireLockAsync(lockKey, lockToken, TimeSpan.FromMinutes(2), ct))
                    return lockToken;
                
                if (i % 10 == 0)
                    logger.LogDebug("Aguardando liberação do recurso {Entity}:{Id}...", EntityName, omieId);
            }
            return null;
        }

        private string GetLockKey(long omieId) => $"sync:{EntityName}:{omieId}";

        private async Task ProcessPedidosBatchAsync(List<OmiePedido> pedidosOmie, CancellationToken ct, Dictionary<long, string>? preAcquiredLocks = null)
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () => 
            {
                // Re-obtem dados do banco a cada tentativa da estratégia para garantir consistência
                var omiePedidoIds = pedidosOmie.Select(p => p.Cabecalho.CodigoPedido).ToList();
                var omieClienteIds = pedidosOmie.Select(p => p.Cabecalho.CodigoCliente).Distinct().ToList();
                var omieProdutoIds = pedidosOmie.SelectMany(p => p.Det.Select(d => d.Produto.CodigoProduto)).Distinct().ToList();
                
                var omieVendedorIds = pedidosOmie
                    .Where(p => p.InformacoesAdicionais?.CodigoVendedor > 0)
                    .Select(p => p.InformacoesAdicionais!.CodigoVendedor!.Value)
                    .Distinct().ToList();

                var omieContaCorrenteIds = pedidosOmie
                    .Where(p => p.InformacoesAdicionais?.CodigoContaCorrente > 0)
                    .Select(p => p.InformacoesAdicionais!.CodigoContaCorrente!.Value)
                    .Distinct().ToList();

                var omieEtapas = pedidosOmie.Select(p => p.Cabecalho.Etapa).Distinct().ToList();
                var omieFormas = pedidosOmie.Where(p => p.Cabecalho.CodigoParcela != null).Select(p => p.Cabecalho.CodigoParcela!).Distinct().ToList();
                var omieCondicoesPagamentoIds = pedidosOmie.Where(p => p.Cabecalho.QuantidadeParcelas > 0).Select(p => (long)p.Cabecalho.QuantidadeParcelas).Distinct().ToList();

                var omieMeioPagamentoCodigos = pedidosOmie.SelectMany(p => p.ListaParcelas?.Parcelas.Where(par => !string.IsNullOrEmpty(par.MeioPagamento)).Select(par => par.MeioPagamento!) ?? Enumerable.Empty<string>()).Distinct().ToList();

                var existingPedidos = await dbContext.PedidosVenda
                    .Include(p => p.Itens)
                    .Include(p => p.Parcelas)
                    .Where(p => omiePedidoIds.Contains(p.OmieId))
                    .ToDictionaryAsync(p => p.OmieId, ct);

                var clientes = await dbContext.Clientes
                    .Where(c => omieClienteIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                var produtos = await dbContext.Produtos
                    .Where(p => omieProdutoIds.Contains(p.OmieId))
                    .ToDictionaryAsync(p => p.OmieId, ct);

                var vendedores = await dbContext.Vendedores
                    .Where(v => omieVendedorIds.Contains(v.OmieId))
                    .ToDictionaryAsync(v => v.OmieId, ct);

                var contasCorrente = await dbContext.ContasCorrente
                    .Where(c => omieContaCorrenteIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                var etapasDict = await dbContext.EtapasFaturamento
                    .Where(e => omieEtapas.Contains(e.Codigo))
                    .ToDictionaryAsync(e => e.Codigo, e => e.Id, ct);

                var formasDict = await dbContext.FormasPagamento
                    .Where(f => omieFormas.Contains(f.Codigo))
                    .ToDictionaryAsync(f => f.Codigo, f => f.Id, ct);

                var condicoesDict = await dbContext.CondicoesPagamento
                    .Where(c => omieCondicoesPagamentoIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, c => c.Id, ct);

                var meiosDict = await dbContext.MeiosPagamento
                    .Where(m => omieMeioPagamentoCodigos.Contains(m.Codigo))
                    .ToDictionaryAsync(m => m.Codigo, m => m.Id, ct);

                var activeLocks = new Dictionary<long, string>();

                try 
                {
                    foreach (var omiePedido in pedidosOmie)
                    {
                        var omieId = omiePedido.Cabecalho.CodigoPedido;

                        string? lockToken;
                        if (preAcquiredLocks != null && preAcquiredLocks.TryGetValue(omieId, out var existingToken))
                        {
                            lockToken = existingToken;
                        }
                        else
                        {
                            // Adquire trava sem espera no lote: pula registro imediatamente se já estiver sendo processado por outro worker
                            lockToken = await AcquireResourceLockAsync(omieId, ct, wait: false);
                        }

                        if (lockToken == null)
                        {
                            logger.LogInformation("Pedido {OmieId} já está sendo processado por outro worker. Pulando no lote.", omieId);
                            continue;
                        }

                        if (preAcquiredLocks == null || !preAcquiredLocks.ContainsKey(omieId))
                        {
                            activeLocks[omieId] = lockToken;
                        }

                        if (!clientes.TryGetValue(omiePedido.Cabecalho.CodigoCliente, out var cliente))
                        {
                            logger.LogWarning("Cliente {Id} no encontrado. Pulando pedido {Ped}.", omiePedido.Cabecalho.CodigoCliente, omiePedido.Cabecalho.NumeroPedido);
                            continue;
                        }

                        existingPedidos.TryGetValue(omieId, out var existingPedido);

                        if (existingPedido == null)
                        {
                            existingPedido = new PedidoVenda
                            {
                                Id = Guid.NewGuid(),
                                OmieId = omieId,
                                CreatedAt = DateTime.UtcNow,
                                Itens = new List<ItemPedido>(),
                                Parcelas = new List<PedidoParcela>()
                            };
                            dbContext.PedidosVenda.Add(existingPedido);
                        }
                        else
                        {
                            var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HInc);
                            if (existingPedido.OmieUpdatedAt.HasValue && omieLastAlt.HasValue && 
                                existingPedido.OmieUpdatedAt.Value == omieLastAlt.Value)
                            {
                                continue;
                            }
                        }

                        // Atualizar campos do pedido
                        existingPedido.NumeroPedido = omiePedido.Cabecalho.NumeroPedido;
                        existingPedido.Etapa = omiePedido.Cabecalho.Etapa;
                        existingPedido.EtapaFaturamentoId = etapasDict.TryGetValue(omiePedido.Cabecalho.Etapa, out var eId) ? eId : null;
                        existingPedido.ValorTotal = omiePedido.TotalPedido.ValorTotalPedido;
                        existingPedido.ClienteId = cliente.Id;
                        existingPedido.CodigoParcela = omiePedido.Cabecalho.CodigoParcela;
                        existingPedido.FormaPagamentoId = omiePedido.Cabecalho.CodigoParcela != null && formasDict.TryGetValue(omiePedido.Cabecalho.CodigoParcela, out var fId) ? fId : null;
                        existingPedido.CondicaoPagamentoId = omiePedido.Cabecalho.QuantidadeParcelas > 0 && condicoesDict.TryGetValue(omiePedido.Cabecalho.QuantidadeParcelas, out var cId) ? cId : null;
                        
                        existingPedido.ValorFrete = omiePedido.Frete?.ValorFrete ?? 0;
                        existingPedido.QuantidadeVolumes = omiePedido.Frete?.QuantidadeVolumes ?? 0;
                        existingPedido.PesoBruto = omiePedido.Frete?.PesoBruto ?? 0;
                        existingPedido.PesoLiquido = omiePedido.Frete?.PesoLiquido ?? 0;
                        existingPedido.Transportadora = omiePedido.Frete?.Transportadora;
                        existingPedido.ValorSeguro = omiePedido.Frete?.ValorSeguro ?? 0;
                        existingPedido.ValorOutrasDespesas = omiePedido.Frete?.OutrasDespesas ?? 0;

                        existingPedido.ObservacoesVenda = omiePedido.Observacoes?.ObservacaoVenda;
                        existingPedido.ObservacoesInternas = omiePedido.InformacoesAdicionais?.ObservacoesInternas;
                        existingPedido.NumeroPedidoCliente = omiePedido.InformacoesAdicionais?.NumeroPedidoCliente;
                        existingPedido.MeioPagamento = omiePedido.Cabecalho.MeioPagamento;
                        existingPedido.QuantidadeParcelas = omiePedido.Cabecalho.QuantidadeParcelas;
                        existingPedido.VendedorId = omiePedido.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omiePedido.InformacoesAdicionais.CodigoVendedor.Value, out var v) ? v.Id : null;
                        existingPedido.ContaCorrenteId = omiePedido.InformacoesAdicionais?.CodigoContaCorrente > 0 && contasCorrente.TryGetValue(omiePedido.InformacoesAdicionais.CodigoContaCorrente.Value, out var cc) ? cc.Id : null;
                        
                        existingPedido.Faturado = omiePedido.InfoCadastro?.Faturado == "S";
                        existingPedido.Cancelado = omiePedido.InfoCadastro?.Cancelado == "S";
                        existingPedido.OmieUpdatedAt = OmieTimestampHelper.ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HAlt);
                        existingPedido.UpdatedAt = DateTime.UtcNow;

                        // Gesto de Itens (Upsert robusto)
                        var currentItems = existingPedido.Itens.ToDictionary(i => i.OmieId);
                        var newOmieIds = omiePedido.Det.Select(d => d.Ide.CodigoItem).ToHashSet();

                        foreach (var item in existingPedido.Itens.Where(i => !newOmieIds.Contains(i.OmieId)).ToList())
                        {
                            existingPedido.Itens.Remove(item);
                        }

                        foreach (var det in omiePedido.Det)
                        {
                            if (produtos.TryGetValue(det.Produto.CodigoProduto, out var produto))
                            {
                                if (!currentItems.TryGetValue(det.Ide.CodigoItem, out var itemEnt))
                                {
                                    itemEnt = new ItemPedido { Id = Guid.NewGuid(), OmieId = det.Ide.CodigoItem, PedidoVendaId = existingPedido.Id, CreatedAt = DateTime.UtcNow };
                                    existingPedido.Itens.Add(itemEnt);
                                }

                                itemEnt.ProdutoId = produto.Id;
                                itemEnt.Quantidade = (int)det.Produto.Quantidade;
                                itemEnt.ValorUnitario = det.Produto.ValorUnitario;
                                itemEnt.ValorTotal = det.Produto.ValorTotal;
                                itemEnt.UnidadeMedida = det.Produto.Unidade;
                                itemEnt.UpdatedAt = DateTime.UtcNow;
                            }
                        }

                        // Gesto de Parcelas (Upsert)
                        if (omiePedido.ListaParcelas?.Parcelas != null)
                        {
                            var currentParcelas = existingPedido.Parcelas.ToDictionary(p => p.NumeroParcela);
                            var newParcelasNums = omiePedido.ListaParcelas.Parcelas.Select(p => p.NumeroParcela).ToHashSet();

                            foreach (var pEnt in existingPedido.Parcelas.Where(p => !newParcelasNums.Contains(p.NumeroParcela)).ToList())
                            {
                                existingPedido.Parcelas.Remove(pEnt);
                            }

                            foreach (var pOmie in omiePedido.ListaParcelas.Parcelas)
                            {
                                if (!currentParcelas.TryGetValue(pOmie.NumeroParcela, out var pEnt))
                                {
                                    pEnt = new PedidoParcela { Id = Guid.NewGuid(), PedidoVendaId = existingPedido.Id, NumeroParcela = pOmie.NumeroParcela };
                                    existingPedido.Parcelas.Add(pEnt);
                                }

                                pEnt.Valor = pOmie.Valor;
                                pEnt.Percentual = pOmie.Percentual;
                                if (DateTime.TryParseExact(pOmie.DataVencimento, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var dtVenc))
                                    pEnt.DataVencimento = DateTime.SpecifyKind(dtVenc, DateTimeKind.Utc);
                            }
                        }
                    } 

                    await dbContext.SaveChangesAsync(ct);
                    logger.LogInformation("Lote de {Count} pedidos sincronizado com sucesso.", pedidosOmie.Count);
                }
                finally 
                {
                    foreach (var kvp in activeLocks)
                    {
                        await lockService.ReleaseLockAsync(GetLockKey(kvp.Key), kvp.Value, ct);
                    }
                }
            });
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
