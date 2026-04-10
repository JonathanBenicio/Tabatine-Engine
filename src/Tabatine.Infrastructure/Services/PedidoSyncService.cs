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

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarPedidosAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);
                
                if (response == null || response.PedidosVenda == null || response.PedidosVenda.Count == 0) break;

                await ProcessPedidosBatchAsync(response.PedidosVenda, ct);
                
                _logger.LogInformation("Página {Pagina} de {Total} de pedidos sincronizada.", pagina, response.TotalDePaginas);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("Pedidos", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Pedidos finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Pedido específico OmieId: {OmieId}", omieId);
            var omiePedido = await _omieClient.ConsultarPedidoAsync(omieId, ct);
            
            if (omiePedido != null)
            {
                await ProcessPedidosBatchAsync(new List<OmiePedido> { omiePedido }, ct);
            }
            else
            {
                _logger.LogWarning("Pedido OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
            }
        }

        private async Task ProcessPedidosBatchAsync(List<OmiePedido> pedidosOmie, CancellationToken ct)
        {
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

            var condicoes = await _dbContext.CondicoesPagamento
                .Where(c => omieCondicoesPagamentoIds.Contains(c.OmieId))
                .Select(c => new { c.OmieId, c.Id })
                .ToListAsync(ct);
            var condicoesDict = condicoes.GroupBy(c => c.OmieId).ToDictionary(g => g.Key, g => g.First().Id);

            var meios = await _dbContext.MeiosPagamento
                .Where(m => omieMeioPagamentoCodigos.Contains(m.Codigo))
                .Select(m => new { m.Codigo, m.Id })
                .ToListAsync(ct);

            var meiosDict = meios.GroupBy(m => m.Codigo).ToDictionary(g => g.Key, g => g.First().Id);

            var processedBatchIds = new HashSet<long>();

            foreach (var omiePedido in pedidosOmie)
            {
                var omieId = omiePedido.Cabecalho.CodigoPedido;

                if (!processedBatchIds.Add(omieId)) continue;

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
                        CondicaoPagamentoId = omiePedido.Cabecalho.QuantidadeParcelas > 0 && condicoesDict.TryGetValue(omiePedido.Cabecalho.QuantidadeParcelas, out var cNovoId) ? cNovoId : null,
                        
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
                        QuantidadeParcelas = omiePedido.Cabecalho.QuantidadeParcelas,
                        Contato = omiePedido.InformacoesAdicionais?.Contato,
                        VendedorId = omiePedido.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omiePedido.InformacoesAdicionais.CodigoVendedor.Value, out var vNovo) ? vNovo.Id : null,
                        ContaCorrenteId = omiePedido.InformacoesAdicionais?.CodigoContaCorrente > 0 && contasCorrente.TryGetValue(omiePedido.InformacoesAdicionais.CodigoContaCorrente.Value, out var ccNovo) ? ccNovo.Id : null,
                        
                        UsuarioInclusao = omiePedido.InfoCadastro?.UsuarioInclusao,
                        UsuarioAlteracao = omiePedido.InfoCadastro?.UsuarioAlteracao,
                        DataInclusao = OmieTimestampHelper.ParseOmieDateTime(omiePedido.InfoCadastro?.DInc, omiePedido.InfoCadastro?.HInc),
                        OmieUpdatedAt = OmieTimestampHelper.ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HAlt),
                        Faturado = omiePedido.InfoCadastro?.Faturado == "S",
                        Cancelado = omiePedido.InfoCadastro?.Cancelado == "S",
                        Devolvido = omiePedido.InfoCadastro?.Devolvido == "S",
                        Autorizado = omiePedido.InfoCadastro?.Autorizado == "S",
                        Denegado = omiePedido.InfoCadastro?.Denegado == "S",

                        ValorIcms = omiePedido.TotalPedido.ValorIcms,
                        ValorIpi = omiePedido.TotalPedido.ValorIpi,
                        ValorPis = omiePedido.TotalPedido.ValorPis,
                        ValorCofins = omiePedido.TotalPedido.ValorCofins,
                        BaseCalculoIcms = omiePedido.TotalPedido.BaseCalculoIcms,
                        ValorMercadorias = omiePedido.TotalPedido.ValorMercadorias,
                        ValorDesconto = omiePedido.TotalPedido.ValorDescontos,
                        ValorIbs = omiePedido.TotalPedido.ValorIbs,
                        ValorCbs = omiePedido.TotalPedido.ValorCbs,

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

                    _dbContext.PedidosVenda.Add(novoPedido);
                    existingPedido = novoPedido;
                }
                else
                {
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HAlt);

                    if (existingPedido.OmieUpdatedAt.HasValue && omieLastAlt.HasValue && 
                        existingPedido.OmieUpdatedAt.Value == omieLastAlt.Value)
                    {
                        continue;
                    }

                    existingPedido.Etapa = omiePedido.Cabecalho.Etapa;
                    existingPedido.EtapaFaturamentoId = etapasDict.TryGetValue(omiePedido.Cabecalho.Etapa, out var eExId) ? eExId : null;
                    existingPedido.ValorTotal = omiePedido.TotalPedido.ValorTotalPedido;
                    existingPedido.CodigoParcela = omiePedido.Cabecalho.CodigoParcela;
                    existingPedido.FormaPagamentoId = omiePedido.Cabecalho.CodigoParcela != null && formasDict.TryGetValue(omiePedido.Cabecalho.CodigoParcela, out var fExId) ? fExId : null;
                    existingPedido.CondicaoPagamentoId = omiePedido.Cabecalho.QuantidadeParcelas > 0 && condicoesDict.TryGetValue(omiePedido.Cabecalho.QuantidadeParcelas, out var cExId) ? cExId : null;
                    
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

                    existingPedido.ValorIcms = omiePedido.TotalPedido.ValorIcms;
                    existingPedido.ValorIpi = omiePedido.TotalPedido.ValorIpi;
                    existingPedido.ValorPis = omiePedido.TotalPedido.ValorPis;
                    existingPedido.ValorCofins = omiePedido.TotalPedido.ValorCofins;
                    existingPedido.BaseCalculoIcms = omiePedido.TotalPedido.BaseCalculoIcms;
                    existingPedido.ValorMercadorias = omiePedido.TotalPedido.ValorMercadorias;
                    existingPedido.ValorDesconto = omiePedido.TotalPedido.ValorDescontos;
                    existingPedido.ValorIbs = omiePedido.TotalPedido.ValorIbs;
                    existingPedido.ValorCbs = omiePedido.TotalPedido.ValorCbs;
                    
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
                    existingPedido.QuantidadeParcelas = omiePedido.Cabecalho.QuantidadeParcelas;
                    existingPedido.ObservacoesVenda = omiePedido.Observacoes?.ObservacaoVenda;
                    existingPedido.DadosAdicionaisNf = omiePedido.InformacoesAdicionais?.DadosAdicionaisNf;
                    existingPedido.NumeroPedidoCliente = omiePedido.InformacoesAdicionais?.NumeroPedidoCliente;
                    existingPedido.ConsumidorFinal = omiePedido.InformacoesAdicionais?.ConsumidorFinal;

                    existingPedido.Faturado = omiePedido.InfoCadastro?.Faturado == "S";
                    existingPedido.Cancelado = omiePedido.InfoCadastro?.Cancelado == "S";
                    existingPedido.Devolvido = omiePedido.InfoCadastro?.Devolvido == "S";
                    existingPedido.Autorizado = omiePedido.InfoCadastro?.Autorizado == "S";
                    existingPedido.Denegado = omiePedido.InfoCadastro?.Denegado == "S";
                    existingPedido.UsuarioAlteracao = omiePedido.InfoCadastro?.UsuarioAlteracao;
                    existingPedido.OmieUpdatedAt = OmieTimestampHelper.ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HAlt);

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

                // Remove itens existentes de forma robusta via contexto para garantir que o EF rastreie a remoção
                var itemsToDelete = await _dbContext.ItensPedido.Where(i => i.PedidoVendaId == existingPedido.Id).ToListAsync(ct);
                if (itemsToDelete.Any())
                {
                    _logger.LogDebug("Removendo {Count} itens existentes do pedido {OmieId} (ID: {Id})", itemsToDelete.Count, existingPedido.OmieId, existingPedido.Id);
                    _dbContext.ItensPedido.RemoveRange(itemsToDelete);
                    await _dbContext.SaveChangesAsync(ct); // Flush deletion to avoid conflicts
                    existingPedido.Itens.Clear();
                }

                foreach (var item in omiePedido.Det)
                {
                    if (produtos.TryGetValue(item.Produto.CodigoProduto, out var produto))
                    {
                        existingPedido.Itens.Add(new ItemPedido
                        {
                            Id = Guid.NewGuid(),
                            OmieId = item.Ide.CodigoItem,
                            PedidoVendaId = existingPedido.Id,
                            ProdutoId = produto.Id,
                            Quantidade = (int)item.Produto.Quantidade,
                            ValorUnitario = item.Produto.ValorUnitario,
                            ValorTotal = item.Produto.ValorTotal,
                            UnidadeMedida = item.Produto.Unidade,
                            Cfop = item.Produto.Cfop,

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
                            CstPis = item.Imposto?.Pis?.CstPis,

                            ValorCofins = item.Imposto?.Cofins?.ValorCofins ?? 0,
                            BaseCofins = item.Imposto?.Cofins?.BaseCalculoCofins ?? 0,
                            AliqCofins = item.Imposto?.Cofins?.AliquotaCofins ?? 0,
                            CstCofins = item.Imposto?.Cofins?.CstCofins,

                            PercentualDesconto = item.Produto.PercentualDesconto,
                            ValorDesconto = item.Produto.ValorDesconto,
                            PesoBruto = item.InfoAdic?.PesoBruto ?? 0,
                            PesoLiquido = item.InfoAdic?.PesoLiquido ?? 0,
                            
                            ValorIbs = item.Imposto?.Ibs?.ValorIbs ?? 0,
                            AliqIbs = item.Imposto?.Ibs?.AliquotaIbs ?? 0,
                            ValorCbs = item.Imposto?.Cbs?.ValorCbs ?? 0,
                            AliqCbs = item.Imposto?.Cbs?.AliquotaCbs ?? 0,
                            BaseIbsCbs = item.Imposto?.IbsCbs?.BaseIbsCbs ?? 0,

                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Remove parcelas existentes de forma robusta
                var parcelasToDelete = await _dbContext.PedidoParcelas.Where(p => p.PedidoVendaId == existingPedido.Id).ToListAsync(ct);
                if (parcelasToDelete.Any())
                {
                    _logger.LogDebug("Removendo {Count} parcelas existentes do pedido {OmieId}", parcelasToDelete.Count, existingPedido.OmieId);
                    _dbContext.PedidoParcelas.RemoveRange(parcelasToDelete);
                    await _dbContext.SaveChangesAsync(ct);
                    existingPedido.Parcelas.Clear();
                }

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
                                MeioPagamentoId = !string.IsNullOrEmpty(parcela.MeioPagamento) && meiosDict.TryGetValue(parcela.MeioPagamento, out var mId) ? mId : null,
                                Nsu = parcela.Nsu,
                                Categoria = parcela.Categoria
                            });
                        }
                    }
                }
            }

            int retries = 0;
            const int maxRetries = 3;
            
            while (retries < maxRetries)
            {
                try
                {
                    await _dbContext.SaveChangesAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    retries++;
                    _logger.LogWarning(ex, "Concorrência detectada no pedido {OmieId}. Tentativa {Retry} de {Max}.", omiePedidoIds, retries, maxRetries);
                    
                    if (retries >= maxRetries) throw;
                    
                    // Recarrega o estado do banco para todas as entidades afetadas e limpa o tracker se necessário
                    foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                    {
                        await entry.ReloadAsync(ct);
                        if (entry.Entity is PedidoVenda pedido)
                        {
                            await _dbContext.Entry(pedido).Collection(p => p.Itens).LoadAsync(ct);
                            await _dbContext.Entry(pedido).Collection(p => p.Parcelas).LoadAsync(ct);
                        }
                    }
                }
            }
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
