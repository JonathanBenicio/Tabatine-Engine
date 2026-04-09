using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models;
using Tabatine.Omie.Client.Models.NotasFiscais;

namespace Tabatine.Infrastructure.Services
{
    public class NotaFiscalSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<NotaFiscalSyncService> logger) : ISyncService
    {
        private readonly IOmieClient _omieClient = omieClient;
        private readonly AppDbContext _dbContext = dbContext;
        private readonly ISyncStateRepository _syncState = syncState;
        private readonly ILogger<NotaFiscalSyncService> _logger = logger;

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Notas Fiscais...");

            var lastSyncDate = await _syncState.GetLastSyncDateAsync("NotasFiscais", ct);
            var syncStartTime = DateTime.UtcNow;

            // Carrega lookups locais exaustivos (Gold Standard)
            var clientesLookup = await _dbContext.Clientes.ToDictionaryAsync(c => c.OmieId, ct);
            var produtosLookup = await _dbContext.Produtos.ToDictionaryAsync(p => p.CodigoProduto, ct);
            var pedidosLookup = await _dbContext.PedidosVenda.ToDictionaryAsync(p => p.OmieId, ct);
            var vendedoresLookup = await _dbContext.Vendedores.ToDictionaryAsync(v => v.OmieId, ct);
            
            // Notas existentes (apenas IDs para controle de insert/update)
            var existingNfIds = await _dbContext.NotasFiscais.Select(n => n.OmieId).ToListAsync(ct);
            var processedOmieIds = new HashSet<long>();
            int count = 0;

            await foreach (var omieNf in _omieClient.StreamNotasFiscaisAsync(filtrarDe: lastSyncDate, cancellationToken: ct))
            {
                var omieId = omieNf.Compl.IdNf;
                if (!processedOmieIds.Add(omieId)) continue;

                if (!clientesLookup.TryGetValue(omieNf.Destinatario.CodigoCliente, out var cliente))
                {
                    _logger.LogWarning("Cliente {Id} não encontrado. Pulando nota {Nf}.", omieNf.Destinatario.CodigoCliente, omieNf.Ide.Numero);
                    continue;
                }

                // Busca a NF completa se existir para tratar itens/títulos
                var existingNf = existingNfIds.Contains(omieId)
                    ? await _dbContext.NotasFiscais
                        .Include(n => n.Itens)
                        .Include(n => n.Titulos)
                        .FirstOrDefaultAsync(n => n.OmieId == omieId, ct)
                    : null;

                pedidosLookup.TryGetValue(omieNf.Compl.IdPedido ?? 0, out var pedido);

                if (existingNf == null)
                {
                    var novaNf = MapToNovaNf(omieNf, cliente, pedido);
                    _dbContext.NotasFiscais.Add(novaNf);
                    UpdateItensETitulos(novaNf, omieNf, produtosLookup, vendedoresLookup, pedido);
                }
                else
                {
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieNf.Info?.DAlt, omieNf.Info?.HAlt);
                    if (existingNf.OmieUpdatedAt.HasValue && omieLastAlt.HasValue && 
                        existingNf.OmieUpdatedAt.Value == omieLastAlt.Value)
                    {
                        continue;
                    }

                    AtualizarNfExistente(existingNf, omieNf, cliente, pedido);
                    UpdateItensETitulos(existingNf, omieNf, produtosLookup, vendedoresLookup, pedido);
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} notas fiscais processadas...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("NotasFiscais", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Notas Fiscais finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Nota Fiscal específica OmieId: {OmieId}", omieId);
            var omieNf = await _omieClient.ConsultarNotaFiscalAsync(omieId, ct);

            if (omieNf != null)
            {
                var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieNf.Destinatario.CodigoCliente, ct);
                if (cliente == null) return;

                var prodIds = omieNf.Det.Select(d => d.Prod.Codigo.ToString()).ToList();
                var prods = await _dbContext.Produtos.Where(p => prodIds.Contains(p.CodigoProduto)).ToDictionaryAsync(p => p.CodigoProduto, ct);
                
                var existing = await _dbContext.NotasFiscais
                    .Include(n => n.Itens)
                    .Include(n => n.Titulos)
                    .FirstOrDefaultAsync(n => n.OmieId == omieId, ct);

                var pedido = omieNf.Compl.IdPedido.HasValue 
                    ? await _dbContext.PedidosVenda.FirstOrDefaultAsync(p => p.OmieId == omieNf.Compl.IdPedido.Value, ct)
                    : null;

                var vendedores = await _dbContext.Vendedores.ToDictionaryAsync(v => v.OmieId, ct);

                if (existing == null)
                {
                    var nova = MapToNovaNf(omieNf, cliente, pedido);
                    _dbContext.NotasFiscais.Add(nova);
                    UpdateItensETitulos(nova, omieNf, prods, vendedores, pedido);
                }
                else
                {
                    AtualizarNfExistente(existing, omieNf, cliente, pedido);
                    UpdateItensETitulos(existing, omieNf, prods, vendedores, pedido);
                }

                await _dbContext.SaveChangesAsync(ct);
            }
        }

        private void UpdateItensETitulos(NotaFiscal nf, OmieNotaFiscal omie, Dictionary<string, Produto> produtos, Dictionary<long, Vendedor> vendedores, PedidoVenda? pedido)
        {
            // Limpa existentes para simplicidade de Upsert
            foreach (var item in nf.Itens.ToList()) _dbContext.ItensNotaFiscal.Remove(item);
            nf.Itens.Clear();

            foreach (var det in omie.Det)
            {
                if (produtos.TryGetValue(det.Prod.Codigo.ToString(), out var produto))
                {
                    nf.Itens.Add(new ItemNotaFiscal
                    {
                        Id = Guid.NewGuid(),
                        NotaFiscalId = nf.Id,
                        ProdutoId = produto.Id,
                        Quantidade = det.Prod.Quantidade,
                        ValorUnitario = det.Prod.ValorUnitario,
                        ValorTotal = det.Prod.ValorTotal,
                        Cfop = det.Prod.Cfop,
                        Ncm = det.Prod.Ncm,
                        BaseIcms = det.Imposto?.Icms?.Base ?? 0,
                        AliqIcms = det.Imposto?.Icms?.Aliquota ?? 0,
                        CstIcms = det.Imposto?.Icms?.Cst,
                        BaseIpi = det.Imposto?.Ipi?.Base ?? 0,
                        AliqIpi = det.Imposto?.Ipi?.Aliquota ?? 0,
                        CstIpi = det.Imposto?.Ipi?.Cst,
                        ValorIpi = det.Imposto?.Ipi?.Valor ?? 0,
                        ValorPis = det.Imposto?.Pis?.Valor ?? 0,
                        ValorCofins = det.Imposto?.Cofins?.Valor ?? 0,
                        ValorIbs = det.Imposto?.Ibs?.ValorIbs ?? 0,
                        AliqIbs = det.Imposto?.Ibs?.AliquotaIbs ?? 0,
                        ValorCbs = det.Imposto?.Cbs?.ValorCbs ?? 0,
                        AliqCbs = det.Imposto?.Cbs?.AliquotaCbs ?? 0,
                        BaseIbsCbs = det.Imposto?.IbsCbs?.BaseIbsCbs ?? 0
                    });
                }
            }

            foreach (var titulo in nf.Titulos.ToList()) _dbContext.NotaFiscalTitulos.Remove(titulo);
            nf.Titulos.Clear();

            if (omie.Titulos != null)
            {
                foreach (var tit in omie.Titulos)
                {
                    if (DateTime.TryParseExact(tit.DataVencimento, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtVenc))
                    {
                        vendedores.TryGetValue(tit.CodigoVendedor, out var titVendedor);
                        nf.Titulos.Add(new NotaFiscalTitulo
                        {
                            Id = Guid.NewGuid(),
                            NotaFiscalId = nf.Id,
                            OmieIdTitulo = tit.OmieIdTitulo,
                            NumeroParcela = tit.Parcela,
                            Valor = tit.Valor,
                            DataVencimento = DateTime.SpecifyKind(dtVenc, DateTimeKind.Utc),
                            ContaCorrenteId = pedido?.ContaCorrenteId,
                            VendedorId = titVendedor?.Id
                        });
                    }
                }
            }
        }

        private NotaFiscal MapToNovaNf(OmieNotaFiscal omie, Cliente cliente, PedidoVenda? pedido)
        {
            var status = StatusHelper(omie.Ide.Situacao, omie.Ide.Denegada == "S");
            var nf = new NotaFiscal
            {
                Id = Guid.NewGuid(),
                OmieId = omie.Compl.IdNf,
                NumeroNf = omie.Ide.Numero,
                ChaveAcesso = omie.Compl.ChaveNfe,
                Status = status,
                CodigoStatus = int.TryParse(omie.Ide.Situacao, out int cs) ? cs : 0,
                NaturezaOperacao = omie.Compl.XNatureza,
                Serie = omie.Ide.Serie,
                Modelo = omie.Ide.Modelo,
                TipoOperacao = omie.Ide.TipoNf,
                Finalidade = omie.Ide.Finalidade,
                Ambiente = omie.Ide.Ambiente,
                InformacoesComplementares = omie.Compl.InformacoesComplementares,
                InformacoesFisco = omie.Compl.InformacoesFisco,
                ImportadoApi = true,
                ValorTotal = omie.Total.IcmsTot.ValorNota,
                ValorFrete = omie.Total.IcmsTot.ValorFrete,
                ValorSeguro = omie.Total.IcmsTot.ValorSeguro,
                ValorDesconto = omie.Total.IcmsTot.ValorDesconto,
                ValorOutrasDespesas = omie.Total.IcmsTot.ValorOutrasDespesas,
                IssqnBaseCalculo = omie.Total.IssqnTot?.BaseCalculo ?? 0,
                ValorIss = omie.Total.IssqnTot?.ValorIss ?? 0,
                ValorIr = omie.Total.RetTrib?.ValorIrrf ?? 0,
                ValorCsll = omie.Total.RetTrib?.ValorCsll ?? 0,
                ValorPisRetido = omie.Total.RetTrib?.ValorPis ?? 0,
                ValorCofinsRetido = omie.Total.RetTrib?.ValorCofins ?? 0,
                ValorIpi = omie.Total.IcmsTot.ValorIpi,
                ValorPis = omie.Total.IcmsTot.ValorPis,
                ValorCofins = omie.Total.IcmsTot.ValorCofins,
                ValorProd = omie.Total.IcmsTot.ValorProdutos,
                IcmsBaseCalculo = omie.Total.IcmsTot.BaseCalculoIcms,
                IcmsValor = omie.Total.IcmsTot.ValorIcms,
                ValorIbs = omie.Total.IcmsTot.ValorIbs,
                ValorCbs = omie.Total.IcmsTot.ValorCbs,
                Denegada = omie.Ide.Denegada == "S",
                ClienteId = cliente.Id,
                PedidoVendaId = pedido?.Id,
                VendedorId = pedido?.VendedorId,
                ContaCorrenteId = pedido?.ContaCorrenteId,
                IdTransportadora = omie.Compl.IdTransportadora,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OmieUpdatedAt = OmieTimestampHelper.ParseOmieDateTime(omie.Info?.DAlt, omie.Info?.HAlt),
                Itens = new List<ItemNotaFiscal>(),
                Titulos = new List<NotaFiscalTitulo>()
            };

            if (DateTime.TryParseExact(omie.Ide.DataEmissao, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dEmi))
            {
                nf.DataEmissao = DateTime.SpecifyKind(dEmi, DateTimeKind.Utc);
            }
            if (TimeSpan.TryParse(omie.Ide.HoraEmissao, out var hEmi)) nf.HoraEmissao = hEmi;
            if (DateTime.TryParseExact(omie.Ide.DataSaida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dSai)) nf.DataSaida = DateTime.SpecifyKind(dSai, DateTimeKind.Utc);
            if (TimeSpan.TryParse(omie.Ide.HoraSaida, out var hSai)) nf.HoraSaida = hSai;

            return nf;
        }

        private void AtualizarNfExistente(NotaFiscal existing, OmieNotaFiscal omie, Cliente cliente, PedidoVenda? pedido)
        {
            var status = StatusHelper(omie.Ide.Situacao, omie.Ide.Denegada == "S");
            existing.Status = status;
            existing.CodigoStatus = int.TryParse(omie.Ide.Situacao, out int csUpd) ? csUpd : 0;
            existing.ChaveAcesso = omie.Compl.ChaveNfe;
            existing.ValorIss = omie.Total.IssqnTot?.ValorIss ?? 0;
            existing.ValorIr = omie.Total.RetTrib?.ValorIrrf ?? 0;
            existing.ValorCsll = omie.Total.RetTrib?.ValorCsll ?? 0;
            existing.ValorPisRetido = omie.Total.RetTrib?.ValorPis ?? 0;
            existing.ValorCofinsRetido = omie.Total.RetTrib?.ValorCofins ?? 0;
            existing.NaturezaOperacao = omie.Compl.XNatureza;
            existing.Serie = omie.Ide.Serie;
            existing.Modelo = omie.Ide.Modelo;
            existing.TipoOperacao = omie.Ide.TipoNf;
            existing.Finalidade = omie.Ide.Finalidade;
            existing.Ambiente = omie.Ide.Ambiente;
            existing.InformacoesComplementares = omie.Compl.InformacoesComplementares;
            existing.InformacoesFisco = omie.Compl.InformacoesFisco;
            existing.ValorFrete = omie.Total.IcmsTot.ValorFrete;
            existing.ValorSeguro = omie.Total.IcmsTot.ValorSeguro;
            existing.ValorDesconto = omie.Total.IcmsTot.ValorDesconto;
            existing.ValorOutrasDespesas = omie.Total.IcmsTot.ValorOutrasDespesas;
            existing.IssqnBaseCalculo = omie.Total.IssqnTot?.BaseCalculo ?? 0;
            existing.ValorIpi = omie.Total.IcmsTot.ValorIpi;
            existing.ValorPis = omie.Total.IcmsTot.ValorPis;
            existing.ValorCofins = omie.Total.IcmsTot.ValorCofins;
            existing.ValorProd = omie.Total.IcmsTot.ValorProdutos;
            existing.IcmsBaseCalculo = omie.Total.IcmsTot.BaseCalculoIcms;
            existing.IcmsValor = omie.Total.IcmsTot.ValorIcms;
            existing.ValorIbs = omie.Total.IcmsTot.ValorIbs;
            existing.ValorCbs = omie.Total.IcmsTot.ValorCbs;
            existing.Denegada = omie.Ide.Denegada == "S";
            existing.PedidoVendaId = pedido?.Id;
            existing.VendedorId = pedido?.VendedorId;
            existing.ContaCorrenteId = pedido?.ContaCorrenteId;
            existing.IdTransportadora = omie.Compl.IdTransportadora;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.OmieUpdatedAt = OmieTimestampHelper.ParseOmieDateTime(omie.Info?.DAlt, omie.Info?.HAlt);

            if (DateTime.TryParseExact(omie.Ide.DataSaida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dSaiU)) existing.DataSaida = DateTime.SpecifyKind(dSaiU, DateTimeKind.Utc);
            else existing.DataSaida = null;
            if (TimeSpan.TryParse(omie.Ide.HoraSaida, out var hSaiU)) existing.HoraSaida = hSaiU;
            else existing.HoraSaida = null;
        }

        private string StatusHelper(string situacao, bool denegada)
        {
            return situacao switch
            {
                "101" => "CANCELADA",
                "110" => "DENEGADA",
                "301" => "DENEGADA",
                _ => denegada ? "DENEGADA" : "AUTORIZADA"
            };
        }
    }
}
