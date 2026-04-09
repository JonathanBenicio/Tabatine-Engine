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

            // Carrega lookups locais exaustivos para evitar N+1 (Gold Standard)
            var clientesLookup = await _dbContext.Clientes.ToDictionaryAsync(c => c.OmieId, ct);
            var produtosLookup = await _dbContext.Produtos.ToDictionaryAsync(p => p.OmieId, ct);
            var vendedoresLookup = await _dbContext.Vendedores.ToDictionaryAsync(v => v.OmieId, ct);
            var contasCorrenteLookup = await _dbContext.ContasCorrente.ToDictionaryAsync(c => c.OmieId, ct);
            var etapasLookup = await _dbContext.EtapasFaturamento.ToDictionaryAsync(e => e.Codigo, ct);
            var formasLookup = await _dbContext.FormasPagamento.ToDictionaryAsync(f => f.Codigo, ct);
            var condicoesLookup = await _dbContext.CondicoesPagamento.ToDictionaryAsync(c => c.OmieId, ct);
            var meiosLookup = await _dbContext.MeiosPagamento.ToDictionaryAsync(m => m.Codigo, ct);
            
            // Pedidos existentes (apenas IDs para decidir entre Update/Insert e limpar itens/parcelas)
            var existingPedidosIds = await _dbContext.PedidosVenda.Select(p => p.OmieId).ToListAsync(ct);
            var processedOmieIds = new HashSet<long>(); // Para idempotência no loop
            int count = 0;

            await foreach (var omiePedido in _omieClient.StreamPedidosAsync(filtrarDe: lastSyncDate, cancellationToken: ct))
            {
                var omieId = omiePedido.Cabecalho.CodigoPedido;
                if (!processedOmieIds.Add(omieId)) continue;

                if (!clientesLookup.TryGetValue(omiePedido.Cabecalho.CodigoCliente, out var cliente))
                {
                    _logger.LogWarning("Cliente {Id} não encontrado. Pulando pedido {Ped}.", omiePedido.Cabecalho.CodigoCliente, omiePedido.Cabecalho.NumeroPedido);
                    continue;
                }

                // Busca o pedido completo se ele existir para tratar itens/parcelas
                var existingPedido = existingPedidosIds.Contains(omieId) 
                    ? await _dbContext.PedidosVenda
                        .Include(p => p.Itens)
                        .Include(p => p.Parcelas)
                        .FirstOrDefaultAsync(p => p.OmieId == omieId, ct)
                    : null;

                if (existingPedido == null)
                {
                    var novoPedido = MapToNovoPedido(omiePedido, cliente, vendedoresLookup, contasCorrenteLookup, etapasLookup, formasLookup, condicoesLookup);
                    _dbContext.PedidosVenda.Add(novoPedido);
                    UpdateItensEParcelas(novoPedido, omiePedido, produtosLookup, meiosLookup);
                }
                else
                {
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omiePedido.InfoCadastro?.DAlt, omiePedido.InfoCadastro?.HAlt);
                    if (existingPedido.OmieUpdatedAt.HasValue && omieLastAlt.HasValue && 
                        existingPedido.OmieUpdatedAt.Value == omieLastAlt.Value)
                    {
                        continue;
                    }

                    AtualizarPedidoExistente(existingPedido, omiePedido, vendedoresLookup, contasCorrenteLookup, etapasLookup, formasLookup, condicoesLookup);
                    UpdateItensEParcelas(existingPedido, omiePedido, produtosLookup, meiosLookup);
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} pedidos processados...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("Pedidos", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Pedidos finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Pedido específico OmieId: {OmieId}", omieId);
            var omiePedido = await _omieClient.ConsultarPedidoAsync(omieId, ct);
            
            if (omiePedido != null)
            {
                // Para SyncById, usamos lookups específicos do pedido para evitar carregar tudo
                var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omiePedido.Cabecalho.CodigoCliente, ct);
                if (cliente == null) return;

                var prodIds = omiePedido.Det.Select(d => d.Produto.CodigoProduto).ToList();
                var prods = await _dbContext.Produtos.Where(p => prodIds.Contains(p.OmieId)).ToDictionaryAsync(p => p.OmieId, ct);
                
                var existing = await _dbContext.PedidosVenda
                    .Include(p => p.Itens)
                    .Include(p => p.Parcelas)
                    .FirstOrDefaultAsync(p => p.OmieId == omieId, ct);

                // Reutilizamos os métodos privados de mapeamento
                // (Note: SyncById acaba sendo menos otimizado que o SyncAll exaustivo, mas é pontual)
                var vendedores = await _dbContext.Vendedores.ToDictionaryAsync(v => v.OmieId, ct);
                var contas = await _dbContext.ContasCorrente.ToDictionaryAsync(c => c.OmieId, ct);
                var etapas = await _dbContext.EtapasFaturamento.ToDictionaryAsync(e => e.Codigo, ct);
                var formas = await _dbContext.FormasPagamento.ToDictionaryAsync(f => f.Codigo, ct);
                var condicoes = await _dbContext.CondicoesPagamento.ToDictionaryAsync(c => c.OmieId, ct);
                var meios = await _dbContext.MeiosPagamento.ToDictionaryAsync(m => m.Codigo, ct);

                if (existing == null)
                {
                    var novo = MapToNovoPedido(omiePedido, cliente, vendedores, contas, etapas, formas, condicoes);
                    _dbContext.PedidosVenda.Add(novo);
                    UpdateItensEParcelas(novo, omiePedido, prods, meios);
                }
                else
                {
                    AtualizarPedidoExistente(existing, omiePedido, vendedores, contas, etapas, formas, condicoes);
                    UpdateItensEParcelas(existing, omiePedido, prods, meios);
                }

                await _dbContext.SaveChangesAsync(ct);
            }
        }

        private void UpdateItensEParcelas(PedidoVenda pedido, OmiePedido omiePedido, Dictionary<long, Produto> produtos, Dictionary<string, MeioPagamento> meios)
        {
            // Limpa existentes para simplicidade de Upsert em entidades dependentes
            foreach (var currentItem in pedido.Itens.ToList()) _dbContext.ItensPedido.Remove(currentItem);
            pedido.Itens.Clear();

            foreach (var item in omiePedido.Det)
            {
                if (produtos.TryGetValue(item.Produto.CodigoProduto, out var produto))
                {
                    pedido.Itens.Add(new ItemPedido
                    {
                        Id = Guid.NewGuid(),
                        OmieId = item.Ide.CodigoItem,
                        PedidoVendaId = pedido.Id,
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

            foreach (var currentParcela in pedido.Parcelas.ToList()) _dbContext.PedidoParcelas.Remove(currentParcela);
            pedido.Parcelas.Clear();

            if (omiePedido.ListaParcelas?.Parcelas != null)
            {
                foreach (var parcela in omiePedido.ListaParcelas.Parcelas)
                {
                    if (DateTime.TryParseExact(parcela.DataVencimento, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtVenc))
                    {
                        pedido.Parcelas.Add(new PedidoParcela
                        {
                            Id = Guid.NewGuid(),
                            PedidoVendaId = pedido.Id,
                            NumeroParcela = parcela.NumeroParcela,
                            Valor = parcela.Valor,
                            DataVencimento = DateTime.SpecifyKind(dtVenc, DateTimeKind.Utc),
                            Percentual = parcela.Percentual,
                            ContaCorrenteId = pedido.ContaCorrenteId,
                            MeioPagamentoId = !string.IsNullOrEmpty(parcela.MeioPagamento) && meios.TryGetValue(parcela.MeioPagamento, out var m) ? m.Id : null,
                            Nsu = parcela.Nsu,
                            Categoria = parcela.Categoria
                        });
                    }
                }
            }
        }

        private PedidoVenda MapToNovoPedido(
            OmiePedido omie, 
            Cliente cliente, 
            Dictionary<long, Vendedor> vendedores, 
            Dictionary<long, ContaCorrente> contas,
            Dictionary<string, EtapaFaturamento> etapas,
            Dictionary<string, FormaPagamento> formas,
            Dictionary<long, CondicaoPagamento> condicoes)
        {
            var pedido = new PedidoVenda
            {
                Id = Guid.NewGuid(),
                OmieId = omie.Cabecalho.CodigoPedido,
                NumeroPedido = omie.Cabecalho.NumeroPedido,
                Etapa = omie.Cabecalho.Etapa,
                EtapaFaturamentoId = etapas.TryGetValue(omie.Cabecalho.Etapa, out var e) ? e.Id : null,
                ValorTotal = omie.TotalPedido.ValorTotalPedido,
                ClienteId = cliente.Id,
                CodigoParcela = omie.Cabecalho.CodigoParcela,
                FormaPagamentoId = omie.Cabecalho.CodigoParcela != null && formas.TryGetValue(omie.Cabecalho.CodigoParcela, out var f) ? f.Id : null,
                CondicaoPagamentoId = omie.Cabecalho.QuantidadeParcelas > 0 && condicoes.TryGetValue(omie.Cabecalho.QuantidadeParcelas, out var c) ? c.Id : null,
                
                ValorFrete = omie.Frete?.ValorFrete ?? 0,
                QuantidadeVolumes = omie.Frete?.QuantidadeVolumes ?? 0,
                PesoBruto = omie.Frete?.PesoBruto ?? 0,
                PesoLiquido = omie.Frete?.PesoLiquido ?? 0,
                Transportadora = omie.Frete?.Transportadora,
                CodigoRastreio = omie.Frete?.CodigoRastreio,
                LinkRastreio = omie.Frete?.LinkRastreio,
                VeiculoProprio = omie.Frete?.VeiculoProprio,
                Placa = omie.Frete?.Placa,
                ValorSeguro = omie.Frete?.ValorSeguro ?? 0,
                ValorOutrasDespesas = omie.Frete?.OutrasDespesas ?? 0,

                ObservacoesVenda = omie.Observacoes?.ObservacaoVenda,
                ObservacoesInternas = omie.InformacoesAdicionais?.ObservacoesInternas,
                DadosAdicionaisNf = omie.InformacoesAdicionais?.DadosAdicionaisNf,
                NumeroPedidoCliente = omie.InformacoesAdicionais?.NumeroPedidoCliente,
                ConsumidorFinal = omie.InformacoesAdicionais?.ConsumidorFinal,
                MeioPagamento = omie.Cabecalho.MeioPagamento,
                QuantidadeParcelas = omie.Cabecalho.QuantidadeParcelas,
                Contato = omie.InformacoesAdicionais?.Contato,
                VendedorId = omie.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omie.InformacoesAdicionais.CodigoVendedor.Value, out var v) ? v.Id : null,
                ContaCorrenteId = omie.InformacoesAdicionais?.CodigoContaCorrente > 0 && contas.TryGetValue(omie.InformacoesAdicionais.CodigoContaCorrente.Value, out var cc) ? cc.Id : null,
                
                UsuarioInclusao = omie.InfoCadastro?.UsuarioInclusao,
                UsuarioAlteracao = omie.InfoCadastro?.UsuarioAlteracao,
                DataInclusao = OmieTimestampHelper.ParseOmieDateTime(omie.InfoCadastro?.DInc, omie.InfoCadastro?.HInc),
                OmieUpdatedAt = OmieTimestampHelper.ParseOmieDateTime(omie.InfoCadastro?.DAlt, omie.InfoCadastro?.HInc), // Corrigido para HInc se HAlt for null
                Faturado = omie.InfoCadastro?.Faturado == "S",
                Cancelado = omie.InfoCadastro?.Cancelado == "S",
                Devolvido = omie.InfoCadastro?.Devolvido == "S",
                Autorizado = omie.InfoCadastro?.Autorizado == "S",
                Denegado = omie.InfoCadastro?.Denegado == "S",

                ValorIcms = omie.TotalPedido.ValorIcms,
                ValorIpi = omie.TotalPedido.ValorIpi,
                ValorPis = omie.TotalPedido.ValorPis,
                ValorCofins = omie.TotalPedido.ValorCofins,
                BaseCalculoIcms = omie.TotalPedido.BaseCalculoIcms,
                ValorMercadorias = omie.TotalPedido.ValorMercadorias,
                ValorDesconto = omie.TotalPedido.ValorDescontos,
                ValorIbs = omie.TotalPedido.ValorIbs,
                ValorCbs = omie.TotalPedido.ValorCbs,
                ValorIss = omie.TotalPedido.ValorIss,
                ValorIr = omie.TotalPedido.ValorIr,
                ValorCsll = omie.TotalPedido.ValorCsll,
                ValorInss = omie.TotalPedido.ValorInss,
                ComissaoVendedor = omie.InformacoesAdicionais?.PercComissao ?? 0,
                FreteModalidade = omie.Frete?.Modalidade,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Itens = new List<ItemPedido>(),
                Parcelas = new List<PedidoParcela>()
            };

            if (DateTime.TryParseExact(omie.Cabecalho.DataPrevisao, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrev))
                pedido.DataPrevisao = DateTime.SpecifyKind(dtPrev, DateTimeKind.Utc);

            if (DateTime.TryParseExact(omie.Frete?.PrevisaoEntrega, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrevEnt))
                pedido.PrevisaoEntrega = DateTime.SpecifyKind(dtPrevEnt, DateTimeKind.Utc);

            return pedido;
        }

        private void AtualizarPedidoExistente(
            PedidoVenda existing, 
            OmiePedido omie, 
            Dictionary<long, Vendedor> vendedores, 
            Dictionary<long, ContaCorrente> contas,
            Dictionary<string, EtapaFaturamento> etapas,
            Dictionary<string, FormaPagamento> formas,
            Dictionary<long, CondicaoPagamento> condicoes)
        {
            existing.Etapa = omie.Cabecalho.Etapa;
            existing.EtapaFaturamentoId = etapas.TryGetValue(omie.Cabecalho.Etapa, out var e) ? e.Id : null;
            existing.ValorTotal = omie.TotalPedido.ValorTotalPedido;
            existing.CodigoParcela = omie.Cabecalho.CodigoParcela;
            existing.FormaPagamentoId = omie.Cabecalho.CodigoParcela != null && formas.TryGetValue(omie.Cabecalho.CodigoParcela, out var f) ? f.Id : null;
            existing.CondicaoPagamentoId = omie.Cabecalho.QuantidadeParcelas > 0 && condicoes.TryGetValue(omie.Cabecalho.QuantidadeParcelas, out var c) ? c.Id : null;
            
            existing.ValorFrete = omie.Frete?.ValorFrete ?? 0;
            existing.QuantidadeVolumes = omie.Frete?.QuantidadeVolumes ?? 0;
            existing.PesoBruto = omie.Frete?.PesoBruto ?? 0;
            existing.PesoLiquido = omie.Frete?.PesoLiquido ?? 0;
            existing.Transportadora = omie.Frete?.Transportadora;
            existing.CodigoRastreio = omie.Frete?.CodigoRastreio;
            existing.LinkRastreio = omie.Frete?.LinkRastreio;
            existing.VeiculoProprio = omie.Frete?.VeiculoProprio;
            existing.Placa = omie.Frete?.Placa;
            existing.ValorSeguro = omie.Frete?.ValorSeguro ?? 0;
            existing.ValorOutrasDespesas = omie.Frete?.OutrasDespesas ?? 0;

            existing.ValorIcms = omie.TotalPedido.ValorIcms;
            existing.ValorIpi = omie.TotalPedido.ValorIpi;
            existing.ValorPis = omie.TotalPedido.ValorPis;
            existing.ValorCofins = omie.TotalPedido.ValorCofins;
            existing.BaseCalculoIcms = omie.TotalPedido.BaseCalculoIcms;
            existing.ValorMercadorias = omie.TotalPedido.ValorMercadorias;
            existing.ValorDesconto = omie.TotalPedido.ValorDescontos;
            existing.ValorIbs = omie.TotalPedido.ValorIbs;
            existing.ValorCbs = omie.TotalPedido.ValorCbs;
            existing.ValorIss = omie.TotalPedido.ValorIss;
            existing.ValorIr = omie.TotalPedido.ValorIr;
            existing.ValorCsll = omie.TotalPedido.ValorCsll;
            existing.ValorInss = omie.TotalPedido.ValorInss;

            existing.ComissaoVendedor = omie.InformacoesAdicionais?.PercComissao ?? 0;
            existing.FreteModalidade = omie.Frete?.Modalidade;
            existing.VendedorId = omie.InformacoesAdicionais?.CodigoVendedor > 0 && vendedores.TryGetValue(omie.InformacoesAdicionais.CodigoVendedor.Value, out var v) ? v.Id : null;
            existing.ContaCorrenteId = omie.InformacoesAdicionais?.CodigoContaCorrente > 0 && contas.TryGetValue(omie.InformacoesAdicionais.CodigoContaCorrente.Value, out var cc) ? cc.Id : null;
            existing.ObservacoesInternas = omie.InformacoesAdicionais?.ObservacoesInternas;
            existing.MeioPagamento = omie.Cabecalho.MeioPagamento;
            existing.QuantidadeParcelas = omie.Cabecalho.QuantidadeParcelas;
            existing.ObservacoesVenda = omie.Observacoes?.ObservacaoVenda;
            existing.DadosAdicionaisNf = omie.InformacoesAdicionais?.DadosAdicionaisNf;
            existing.NumeroPedidoCliente = omie.InformacoesAdicionais?.NumeroPedidoCliente;
            existing.ConsumidorFinal = omie.InformacoesAdicionais?.ConsumidorFinal;

            existing.Faturado = omie.InfoCadastro?.Faturado == "S";
            existing.Cancelado = omie.InfoCadastro?.Cancelado == "S";
            existing.Devolvido = omie.InfoCadastro?.Devolvido == "S";
            existing.Autorizado = omie.InfoCadastro?.Autorizado == "S";
            existing.Denegado = omie.InfoCadastro?.Denegado == "S";
            existing.UsuarioAlteracao = omie.InfoCadastro?.UsuarioAlteracao;
            existing.OmieUpdatedAt = OmieTimestampHelper.ParseOmieDateTime(omie.InfoCadastro?.DAlt, omie.InfoCadastro?.HAlt);

            if (DateTime.TryParseExact(omie.Frete?.PrevisaoEntrega, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrevEntEx))
                existing.PrevisaoEntrega = DateTime.SpecifyKind(dtPrevEntEx, DateTimeKind.Utc);

            if (DateTime.TryParseExact(omie.Cabecalho.DataPrevisao, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dtPrevEx))
                existing.DataPrevisao = DateTime.SpecifyKind(dtPrevEx, DateTimeKind.Utc);

            existing.UpdatedAt = DateTime.UtcNow;
        }

    }
}
