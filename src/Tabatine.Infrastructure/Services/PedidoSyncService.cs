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
                    .Where(p => p.InformacoesAdicionais?.CodigoContaCorrente > 0)
                    .Select(p => p.InformacoesAdicionais!.CodigoContaCorrente!.Value)
                    .Distinct().ToList();

                var omieEtapas = response.PedidosVenda.Select(p => p.Cabecalho.Etapa).Distinct().ToList();
                var omieFormas = response.PedidosVenda.Where(p => p.Cabecalho.CodigoParcela != null).Select(p => p.Cabecalho.CodigoParcela!).Distinct().ToList();

                var omieTabelaIds = response.PedidosVenda.SelectMany(p => p.Det.Where(d => d.Produto.CodigoTabelaPreco > 0).Select(d => d.Produto.CodigoTabelaPreco!.Value)).Distinct().ToList();
                var omieMeioPagamentoCodigos = response.PedidosVenda.SelectMany(p => p.ListaParcelas?.Parcelas.Where(par => !string.IsNullOrEmpty(par.MeioPagamento)).Select(par => par.MeioPagamento!) ?? Enumerable.Empty<string>()).Distinct().ToList();

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

                var etapas = await _dbContext.EtapasFaturamento
                    .Where(e => omieEtapas.Contains(e.Codigo))
                    .Select(e => new { e.Codigo, e.Id })
                    .ToListAsync(ct);
                var etapasDict = etapas.GroupBy(e => e.Codigo).ToDictionary(g => g.Key, g => g.First().Id);

                var formas = await _dbContext.FormasPagamento
                    .Where(f => omieFormas.Contains(f.Codigo))
                    .Select(f => new { f.Codigo, f.Id })
                    .ToListAsync(ct);
                var formasDict = formas.GroupBy(f => f.Codigo).ToDictionary(g => g.Key, g => g.First().Id);

                var tabelas = await _dbContext.TabelasPreco
                    .Where(t => omieTabelaIds.Contains(t.OmieId))
                    .Select(t => new { t.OmieId, t.Id })
                    .ToListAsync(ct);
                var tabelasDict = tabelas.GroupBy(t => t.OmieId).ToDictionary(g => g.Key, g => g.First().Id);

                var meios = await _dbContext.MeiosPagamento
                    .Where(m => omieMeioPagamentoCodigos.Contains(m.Codigo))
                    .Select(m => new { m.Codigo, m.Id })
                    .ToListAsync(ct);
                var meiosDict = meios.GroupBy(m => m.Codigo).ToDictionary(g => g.Key, g => g.First().Id);

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
                            EtapaFaturamentoId = etapasDict.TryGetValue(omiePedido.Cabecalho.Etapa, out var eNovoId) ? eNovoId : null,
                            ValorTotal = omiePedido.TotalPedido.ValorTotalPedido,
                            ClienteId = cliente.Id,
                            CodigoParcela = omiePedido.Cabecalho.CodigoParcela,
                            FormaPagamentoId = omiePedido.Cabecalho.CodigoParcela != null && formasDict.TryGetValue(omiePedido.Cabecalho.CodigoParcela, out var fNovoId) ? fNovoId : null,
                            
                            // Frete e Logística estendido
                            ValorFrete = omiePedido.Frete?.ValorFrete ?? 0,
                            QuantidadeVolumes = omiePedido.Frete?.QuantidadeVolumes ?? 0,
                            PesoBruto = omiePedido.Frete?.PesoBruto ?? 0,
                            PesoLiquido = omiePedido.Frete?.PesoLiquido ?? 0,
                            Transportadora = omiePedido.Frete?.Transportadora,
                            CodigoRastreio = omiePedido.Frete?.CodigoRastreio,
                            LinkRastreio = omiePedido.Frete?.LinkRastreio,
                            VeiculoProprio = omiePedido.Frete?.VeiculoProprio,
                            Placa = omiePedido.Frete?.Placa,
                            ValorSeguro = omiePedido.Frete?.ValorSeguro ?? 0,
                            ValorOutrasDespesas = omiePedido.Frete?.OutrasDespesas ?? 0,

                            ObservacoesVenda = omiePedido.Observacoes?.ObservacaoVenda,
                            ObservacoesInternas = omiePedido.InformacoesAdicionais?.ObservacoesInternas,
                            DadosAdicionaisNf = omiePedido.InformacoesAdicionais?.DadosAdicionaisNf,
                            NumeroPedidoCliente = omiePedido.InformacoesAdicionais?.NumeroPedidoCliente,
                            ConsumidorFinal = omiePedido.InformacoesAdicionais?.ConsumidorFinal,
                            MeioPagamento = omiePedido.Cabecalho.MeioPagamento,
                            Contato = omiePedido.InformacoesAdicionais?.Contato,
                            VendedorId = omiePedido.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omiePedido.InformacoesAdicionais.CodigoVendedor.Value, out var vNovo) ? vNovo.Id : null,
                            ContaCorrenteId = omiePedido.InformacoesAdicionais?.CodigoContaCorrente > 0 && contasCorrente.TryGetValue(omiePedido.InformacoesAdicionais.CodigoContaCorrente.Value, out var ccNovo) ? ccNovo.Id : null,
                            
                            // Auditoria e Status
                            UsuarioInclusao = omiePedido.InfoCadastro?.UsuarioInclusao,
                            UsuarioAlteracao = omiePedido.InfoCadastro?.UsuarioAlteracao,
                            DataInclusao = ParseOmieDateTime(omiePedido.InfoCadastro?.DInc, omiePedido.InfoCadastro?.HInc),
                            OmieUpdatedAt = ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HAlt),
                            Faturado = omiePedido.InfoCadastro?.Faturado == "S",
                            Cancelado = omiePedido.InfoCadastro?.Cancelado == "S",
                            Devolvido = omiePedido.InfoCadastro?.Devolvido == "S",
                            Autorizado = omiePedido.InfoCadastro?.Autorizado == "S",
                            Denegado = omiePedido.InfoCadastro?.Denegado == "S",

                            // Impostos Totais
                            ValorIcms = omiePedido.TotalPedido.ValorIcms,
                            ValorIpi = omiePedido.TotalPedido.ValorIpi,
                            ValorPis = omiePedido.TotalPedido.ValorPis,
                            ValorCofins = omiePedido.TotalPedido.ValorCofins,
                            BaseCalculoIcms = omiePedido.TotalPedido.BaseCalculoIcms,
                            ValorMercadorias = omiePedido.TotalPedido.ValorMercadorias,
                            ValorDesconto = omiePedido.TotalPedido.ValorDescontos,
                            ValorIbs = omiePedido.TotalPedido.ValorIbs,
                            ValorCbs = omiePedido.TotalPedido.ValorCbs,

                            // Retenções no Pedido
                            ValorIss = omiePedido.TotalPedido.ValorIss,
                            ValorIr = omiePedido.TotalPedido.ValorIr,
                            ValorCsll = omiePedido.TotalPedido.ValorCsll,
                            ValorInss = omiePedido.TotalPedido.ValorInss,

                            ComissaoVendedor = omiePedido.InformacoesAdicionais?.PercComissao ?? 0,
                            FreteModalidade = omiePedido.Frete?.Modalidade,

                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            Itens = new List<ItemPedido>(),
                            Parcelas = new List<PedidoParcela>()
                        };

                        if (DateTime.TryParseExact(omiePedido.Cabecalho.DataPrevisao, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrev))
                        {
                            novoPedido.DataPrevisao = DateTime.SpecifyKind(dtPrev, DateTimeKind.Utc);
                        }

                        if (DateTime.TryParseExact(omiePedido.Frete?.PrevisaoEntrega, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrevEnt))
                        {
                            novoPedido.PrevisaoEntrega = DateTime.SpecifyKind(dtPrevEnt, DateTimeKind.Utc);
                        }

                        // Removido parsing simples de DataInclusao (já feito acima no construtor)

                        _dbContext.PedidosVenda.Add(novoPedido);
                        existingPedido = novoPedido;
                    }
                    else
                    {
                        existingPedido.Etapa = omiePedido.Cabecalho.Etapa;
                        existingPedido.EtapaFaturamentoId = etapasDict.TryGetValue(omiePedido.Cabecalho.Etapa, out var eExId) ? eExId : null;
                        existingPedido.ValorTotal = omiePedido.TotalPedido.ValorTotalPedido;
                        existingPedido.CodigoParcela = omiePedido.Cabecalho.CodigoParcela;
                        existingPedido.FormaPagamentoId = omiePedido.Cabecalho.CodigoParcela != null && formasDict.TryGetValue(omiePedido.Cabecalho.CodigoParcela, out var fExId) ? fExId : null;
                        
                        // Frete e Logística
                        existingPedido.ValorFrete = omiePedido.Frete?.ValorFrete ?? 0;
                        existingPedido.QuantidadeVolumes = omiePedido.Frete?.QuantidadeVolumes ?? 0;
                        existingPedido.PesoBruto = omiePedido.Frete?.PesoBruto ?? 0;
                        existingPedido.PesoLiquido = omiePedido.Frete?.PesoLiquido ?? 0;
                        existingPedido.Transportadora = omiePedido.Frete?.Transportadora;
                        existingPedido.CodigoRastreio = omiePedido.Frete?.CodigoRastreio;
                        existingPedido.LinkRastreio = omiePedido.Frete?.LinkRastreio;
                        existingPedido.VeiculoProprio = omiePedido.Frete?.VeiculoProprio;
                        existingPedido.Placa = omiePedido.Frete?.Placa;
                        existingPedido.ValorSeguro = omiePedido.Frete?.ValorSeguro ?? 0;
                        existingPedido.ValorOutrasDespesas = omiePedido.Frete?.OutrasDespesas ?? 0;

                        // Impostos Totais
                        existingPedido.ValorIcms = omiePedido.TotalPedido.ValorIcms;
                        existingPedido.ValorIpi = omiePedido.TotalPedido.ValorIpi;
                        existingPedido.ValorPis = omiePedido.TotalPedido.ValorPis;
                        existingPedido.ValorCofins = omiePedido.TotalPedido.ValorCofins;
                        existingPedido.BaseCalculoIcms = omiePedido.TotalPedido.BaseCalculoIcms;
                        existingPedido.ValorMercadorias = omiePedido.TotalPedido.ValorMercadorias;
                        existingPedido.ValorDesconto = omiePedido.TotalPedido.ValorDescontos;
                        existingPedido.ValorIbs = omiePedido.TotalPedido.ValorIbs;
                        existingPedido.ValorCbs = omiePedido.TotalPedido.ValorCbs;
                        
                        // Retenções no Pedido
                        existingPedido.ValorIss = omiePedido.TotalPedido.ValorIss;
                        existingPedido.ValorIr = omiePedido.TotalPedido.ValorIr;
                        existingPedido.ValorCsll = omiePedido.TotalPedido.ValorCsll;
                        existingPedido.ValorInss = omiePedido.TotalPedido.ValorInss;

                        existingPedido.ComissaoVendedor = omiePedido.InformacoesAdicionais?.PercComissao ?? 0;
                        existingPedido.FreteModalidade = omiePedido.Frete?.Modalidade;
                        existingPedido.VendedorId = omiePedido.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omiePedido.InformacoesAdicionais.CodigoVendedor.Value, out var vEx) ? vEx.Id : null;
                        existingPedido.ContaCorrenteId = omiePedido.InformacoesAdicionais?.CodigoContaCorrente > 0 && contasCorrente.TryGetValue(omiePedido.InformacoesAdicionais.CodigoContaCorrente.Value, out var ccEx) ? ccEx.Id : null;
                        existingPedido.ObservacoesInternas = omiePedido.InformacoesAdicionais?.ObservacoesInternas;
                        existingPedido.MeioPagamento = omiePedido.Cabecalho.MeioPagamento;
                        existingPedido.ObservacoesVenda = omiePedido.Observacoes?.ObservacaoVenda;
                        existingPedido.DadosAdicionaisNf = omiePedido.InformacoesAdicionais?.DadosAdicionaisNf;
                        existingPedido.NumeroPedidoCliente = omiePedido.InformacoesAdicionais?.NumeroPedidoCliente;
                        existingPedido.ConsumidorFinal = omiePedido.InformacoesAdicionais?.ConsumidorFinal;

                        // Auditoria e Status
                        existingPedido.Faturado = omiePedido.InfoCadastro?.Faturado == "S";
                        existingPedido.Cancelado = omiePedido.InfoCadastro?.Cancelado == "S";
                        existingPedido.Devolvido = omiePedido.InfoCadastro?.Devolvido == "S";
                        existingPedido.Autorizado = omiePedido.InfoCadastro?.Autorizado == "S";
                        existingPedido.Denegado = omiePedido.InfoCadastro?.Denegado == "S";
                        existingPedido.UsuarioAlteracao = omiePedido.InfoCadastro?.UsuarioAlteracao;
                        existingPedido.OmieUpdatedAt = ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HAlt);

                        if (DateTime.TryParseExact(omiePedido.Frete?.PrevisaoEntrega, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrevEntEx))
                        {
                            existingPedido.PrevisaoEntrega = DateTime.SpecifyKind(dtPrevEntEx, DateTimeKind.Utc);
                        }

                        if (DateTime.TryParseExact(omiePedido.Cabecalho.DataPrevisao, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrevEx))
                        {
                            existingPedido.DataPrevisao = DateTime.SpecifyKind(dtPrevEx, DateTimeKind.Utc);
                        }

                        existingPedido.UpdatedAt = DateTime.UtcNow;
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
                                UnidadeMedida = item.Produto.Unidade,

                                ValorIcms = item.Imposto?.Icms?.ValorIcms ?? 0,
                                BaseIcms = item.Imposto?.Icms?.BaseCalculoIcms ?? 0,
                                AliqIcms = item.Imposto?.Icms?.AliquotaIcms ?? 0,
                                CstIcms = item.Imposto?.Icms?.CstIcms,

                                ValorIpi = item.Imposto?.Ipi?.ValorIpi ?? 0,
                                BaseIpi = item.Imposto?.Ipi?.BaseCalculoIpi ?? 0,
                                AliqIpi = item.Imposto?.Ipi?.AliquotaIpi ?? 0,
                                CstIpi = item.Imposto?.Ipi?.CstIpi,

                                ValorPis = item.Imposto?.Pis?.ValorPis ?? 0,
                                BasePis = item.Imposto?.Pis?.BaseCalculoPis ?? 0,
                                AliqPis = item.Imposto?.Pis?.AliquotaPis ?? 0,

                                ValorCofins = item.Imposto?.Cofins?.ValorCofins ?? 0,
                                BaseCofins = item.Imposto?.Cofins?.BaseCalculoCofins ?? 0,
                                AliqCofins = item.Imposto?.Cofins?.AliquotaCofins ?? 0,

                                PercentualDesconto = item.Produto.PercentualDesconto,
                                ValorDesconto = item.Produto.ValorDesconto,
                                PesoBruto = item.InfoAdic?.PesoBruto ?? 0,
                                PesoLiquido = item.InfoAdic?.PesoLiquido ?? 0,
                                TabelaPrecoId = item.Produto.CodigoTabelaPreco.HasValue && tabelasDict.TryGetValue(item.Produto.CodigoTabelaPreco.Value, out var tId) ? tId : null,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
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
                                    ContaCorrenteId = existingPedido.ContaCorrenteId,
                                    MeioPagamentoId = !string.IsNullOrEmpty(parcela.MeioPagamento) && meiosDict.TryGetValue(parcela.MeioPagamento, out var mId) ? mId : null
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
                catch (DbUpdateConcurrencyException)
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
        private DateTime? ParseOmieDateTime(string? date, string? time)
        {
            if (string.IsNullOrWhiteSpace(date)) return null;
            
            var combined = string.IsNullOrWhiteSpace(time) ? date : $"{date} {time}";
            var format = string.IsNullOrWhiteSpace(time) ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm:ss";

            if (DateTime.TryParseExact(combined, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }

            return null;
        }
    }
}
