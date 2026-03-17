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

namespace Tabatine.Infrastructure.Services
{
    public class PedidoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<PedidoSyncService> _logger;

        public PedidoSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<PedidoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Pedidos de Venda...");
            
            var lastSyncDate = await _syncState.GetLastSyncDateAsync("Pedidos", ct);
            var syncStartTime = DateTime.UtcNow;

            // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
            var processedOmieIds = new HashSet<long>();

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarPedidosAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);
                
                // Resposta nula = sem registros (Client-5113)
                if (response == null || response.PedidosVenda == null || response.PedidosVenda.Count == 0) break;

                var omiePedidoIds = response.PedidosVenda.Select(p => p.Cabecalho.CodigoPedido).ToList();
                var omieClienteIds = response.PedidosVenda.Select(p => p.Cabecalho.CodigoCliente).Distinct().ToList();
                var omieProdutoIds = response.PedidosVenda.SelectMany(p => p.Det.Select(d => d.Produto.CodigoProduto)).Distinct().ToList();
                
                var omieVendedorIds = response.PedidosVenda
                    .Where(p => p.InformacoesAdicionais?.CodigoVendedor > 0)
                    .Select(p => p.InformacoesAdicionais!.CodigoVendedor!.Value)
                    .Distinct().ToList();

                var omieContaCorrenteIds = response.PedidosVenda
                    .Where(p => p.Cabecalho.CodigoContaCorrente > 0)
                    .Select(p => p.Cabecalho.CodigoContaCorrente!.Value)
                    .Distinct().ToList();

                var existingPedidos = await _dbContext.PedidosVenda
                    .Include(p => p.Itens)
                    .Include(p => p.Parcelas)
                    .Where(p => omiePedidoIds.Contains(p.OmieId))
                    .ToDictionaryAsync(p => p.OmieId, ct);

                var clientes = await _dbContext.Clientes
                    .Where(c => omieClienteIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                var produtos = await _dbContext.Produtos
                    .Where(p => omieProdutoIds.Contains(p.OmieId))
                    .ToDictionaryAsync(p => p.OmieId, ct);

                var vendedores = await _dbContext.Vendedores
                    .Where(v => omieVendedorIds.Contains(v.OmieId))
                    .ToDictionaryAsync(v => v.OmieId, ct);

                var contasCorrente = await _dbContext.ContasCorrente
                    .Where(c => omieContaCorrenteIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                foreach (var omiePedido in response.PedidosVenda)
                {
                    var omieId = omiePedido.Cabecalho.CodigoPedido;

                    // Pula se já processamos este OmieId neste ciclo
                    if (!processedOmieIds.Add(omieId))
                    {
                        _logger.LogDebug("Pedido OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
                        continue;
                    }

                    if (!clientes.TryGetValue(omiePedido.Cabecalho.CodigoCliente, out var cliente))
                    {
                        _logger.LogWarning("Cliente {Id} não encontrado. Pulando pedido {Ped}.", omiePedido.Cabecalho.CodigoCliente, omiePedido.Cabecalho.NumeroPedido);
                        continue;
                    }

                    existingPedidos.TryGetValue(omieId, out var existingPedido);

                    if (existingPedido == null)
                    {
                        var novoPedido = new PedidoVenda
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieId,
                            NumeroPedido = omiePedido.Cabecalho.NumeroPedido,
                            Etapa = omiePedido.Cabecalho.Etapa,
                            ValorTotal = omiePedido.TotalPedido.ValorTotalPedido,
                            ClienteId = cliente.Id,
                            ValorFrete = omiePedido.Frete?.ValorFrete ?? 0,
                            Transportadora = omiePedido.Frete?.Transportadora,
                            QuantidadeVolumes = omiePedido.Frete?.QuantidadeVolumes ?? 0,
                            ObservacoesVenda = omiePedido.InformacoesAdicionais?.ObservacoesVenda,
                            VendedorId = omiePedido.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omiePedido.InformacoesAdicionais.CodigoVendedor.Value, out var vNovo) ? vNovo.Id : null,
                            ContaCorrenteId = omiePedido.Cabecalho.CodigoContaCorrente > 0 && contasCorrente.TryGetValue(omiePedido.Cabecalho.CodigoContaCorrente.Value, out var ccNovo) ? ccNovo.Id : null,
                            UsuarioInclusao = omiePedido.InfoCadastro?.UsuarioInclusao,
                            Faturado = omiePedido.InfoCadastro?.Faturado == "S",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Itens = new List<ItemPedido>(),
                            Parcelas = new List<PedidoParcela>()
                        };

                        if (DateTime.TryParseExact(omiePedido.Cabecalho.DataPrevisao, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrev))
                        {
                            novoPedido.DataPrevisao = DateTime.SpecifyKind(dtPrev, DateTimeKind.Utc);
                        }

                        _dbContext.PedidosVenda.Add(novoPedido);
                        existingPedido = novoPedido;
                    }
                    else
                    {
                        existingPedido.Etapa = omiePedido.Cabecalho.Etapa;
                        existingPedido.ValorTotal = omiePedido.TotalPedido.ValorTotalPedido;
                        existingPedido.ValorFrete = omiePedido.Frete?.ValorFrete ?? 0;
                        existingPedido.Transportadora = omiePedido.Frete?.Transportadora;
                        existingPedido.QuantidadeVolumes = omiePedido.Frete?.QuantidadeVolumes ?? 0;
                        existingPedido.ObservacoesVenda = omiePedido.InformacoesAdicionais?.ObservacoesVenda;
                        existingPedido.VendedorId = omiePedido.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omiePedido.InformacoesAdicionais.CodigoVendedor.Value, out var vEx) ? vEx.Id : null;
                        existingPedido.ContaCorrenteId = omiePedido.Cabecalho.CodigoContaCorrente > 0 && contasCorrente.TryGetValue(omiePedido.Cabecalho.CodigoContaCorrente.Value, out var ccEx) ? ccEx.Id : null;
                        existingPedido.UsuarioInclusao = omiePedido.InfoCadastro?.UsuarioInclusao;
                        existingPedido.Faturado = omiePedido.InfoCadastro?.Faturado == "S";
                        existingPedido.UpdatedAt = DateTime.UtcNow;

                        if (DateTime.TryParseExact(omiePedido.Cabecalho.DataPrevisao, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrev))
                        {
                            existingPedido.DataPrevisao = DateTime.SpecifyKind(dtPrev, DateTimeKind.Utc);
                        }
                    }

                    // Reconciliação unificada de itens e parcelas
                    
                    // Itens
                    foreach (var currentItem in existingPedido.Itens.ToList())
                    {
                        _dbContext.ItensPedido.Remove(currentItem);
                    }
                    existingPedido.Itens.Clear();

                    foreach (var item in omiePedido.Det)
                    {
                        if (produtos.TryGetValue(item.Produto.CodigoProduto, out var produto))
                        {
                            existingPedido.Itens.Add(new ItemPedido
                            {
                                Id = Guid.NewGuid(),
                                PedidoVendaId = existingPedido.Id,
                                ProdutoId = produto.Id,
                                Quantidade = (int)item.Produto.Quantidade,
                                ValorUnitario = item.Produto.ValorUnitario,
                                ValorTotal = item.Produto.ValorTotal,
                                ValorIcms = item.Imposto?.Icms?.ValorIcms ?? 0,
                                ValorIpi = item.Imposto?.Ipi?.ValorIpi ?? 0,
                                ValorPis = item.Imposto?.Pis?.ValorPis ?? 0,
                                ValorCofins = item.Imposto?.Cofins?.ValorCofins ?? 0,
                                PercentualDesconto = item.Produto.PercentualDesconto,
                                ValorDesconto = item.Produto.ValorDesconto,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    // Parcelas
                    foreach (var currentParcela in existingPedido.Parcelas.ToList())
                    {
                        _dbContext.PedidoParcelas.Remove(currentParcela);
                    }
                    existingPedido.Parcelas.Clear();

                    if (omiePedido.ListaParcelas?.Parcelas != null)
                    {
                        foreach (var parcela in omiePedido.ListaParcelas.Parcelas)
                        {
                            if (DateTime.TryParseExact(parcela.DataVencimento, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtVenc))
                            {
                                existingPedido.Parcelas.Add(new PedidoParcela
                                {
                                    Id = Guid.NewGuid(),
                                    PedidoVendaId = existingPedido.Id,
                                    NumeroParcela = parcela.NumeroParcela,
                                    Valor = parcela.Valor,
                                    DataVencimento = DateTime.SpecifyKind(dtVenc, DateTimeKind.Utc),
                                    Percentual = parcela.Percentual,
                                    ContaCorrenteId = existingPedido.ContaCorrenteId
                                });
                            }
                        }
                    }
                }

                try
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("Página {Pagina} de {Total} de pedidos sincronizada.", pagina, response.TotalDePaginas);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning("Concorrência detectada ao salvar página {Pagina} de pedidos. Limpando rastreador e ignorando conflito.", pagina);
                    
                    // Crucial: Limpa as entradas rastreadas que falharam para não "poluir" a próxima página
                    foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                    {
                        entry.State = EntityState.Detached;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro inesperado ao salvar página {Pagina} de pedidos. Abortando ciclo para segurança.", pagina);
                    throw; // Re-throw para o SyncManager lidar e registrar falha do serviço
                }
                
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("Pedidos", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Pedidos finalizada.");
        }
    }
}
