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

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Notas Fiscais...");

            DateTime? lastSyncDate = await syncState.GetLastSyncDateAsync("NotasFiscais", ct);
            DateTime syncStartTime = DateTime.UtcNow;

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                ListarNotasFiscaisResponse response = await omieClient.ListarNotasFiscaisAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                if (response == null || response.NotasFiscais == null || response.NotasFiscais.Count == 0) break;

                await ProcessNotaFiscalBatchAsync(response.NotasFiscais, ct);

                logger.LogInformation("Página {Pagina} de {Total} de Notas Fiscais sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await syncState.SetLastSyncDateAsync("NotasFiscais", syncStartTime, ct);
            logger.LogInformation("Sincronização de Notas Fiscais finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogInformation("Sincronizando Nota Fiscal específica OmieId: {OmieId}", omieId);
            var omieNf = await omieClient.ConsultarNotaFiscalAsync(omieId, ct);

            if (omieNf != null)
            {
                await ProcessNotaFiscalBatchAsync(new List<OmieNotaFiscal> { omieNf }, ct);
            }
            else
            {
                logger.LogWarning("Nota Fiscal OmieId {OmieId} não encontrada na Omie para consulta individual.", omieId);
            }
        }

        private async Task ProcessNotaFiscalBatchAsync(List<OmieNotaFiscal> notasFiscaisOmie, CancellationToken ct)
        {
            List<long> omieNfIds = notasFiscaisOmie.Select(n => n.Compl.IdNf).ToList();
            List<long> omieClienteIds = notasFiscaisOmie.Select(n => n.Destinatario.CodigoCliente).Distinct().ToList();
            List<long> omiePedidoIds = notasFiscaisOmie.Where(n => n.Compl.IdPedido.HasValue).Select(n => n.Compl.IdPedido!.Value).Distinct().ToList();

            List<long> omieVendedorIds = notasFiscaisOmie
                .Where(n => n.Titulos != null)
                .SelectMany(n => n.Titulos!)
                .Select(t => t.CodigoVendedor)
                .Where(id => id > 0)
                .Distinct().ToList();

            Dictionary<long, NotaFiscal> existingNfs = await dbContext.NotasFiscais
                        .Include(n => n.Itens)
                        .Include(n => n.Titulos)
                        .Where(n => omieNfIds.Contains(n.OmieId))
                        .ToDictionaryAsync(n => n.OmieId, ct);

            Dictionary<long, Cliente> clientes = await dbContext.Clientes
                        .Where(c => omieClienteIds.Contains(c.OmieId))
                        .ToDictionaryAsync(c => c.OmieId, ct);

            var allOmieProdIds = notasFiscaisOmie.SelectMany(n => n.Det.Select(d => d.Prod.Codigo.ToString())).Distinct().ToList();
            var produtos = await dbContext.Produtos
                        .Where(p => allOmieProdIds.Contains(p.CodigoProduto))
                        .ToDictionaryAsync(p => p.CodigoProduto, ct);

            Dictionary<long, PedidoVenda> pedidos = await dbContext.PedidosVenda
                        .Where(p => omiePedidoIds.Contains(p.OmieId))
                        .ToDictionaryAsync(p => p.OmieId, ct);

            Dictionary<long, Vendedor> vendedores = await dbContext.Vendedores
                        .Where(v => omieVendedorIds.Contains(v.OmieId))
                        .ToDictionaryAsync(v => v.OmieId, ct);

            HashSet<long> processedBatchIds = new HashSet<long>();

            foreach (OmieNotaFiscal omieNf in notasFiscaisOmie)
            {
                long omieId = omieNf.Compl.IdNf;

                if (!processedBatchIds.Add(omieId)) continue;

                if (!clientes.TryGetValue(omieNf.Destinatario.CodigoCliente, out Cliente? cliente)) continue;

                _ = pedidos.TryGetValue(omieNf.Compl.IdPedido ?? 0, out PedidoVenda? pedido);
                _ = existingNfs.TryGetValue(omieId, out NotaFiscal? existing);

                DateTime dataEmissao = DateTime.UtcNow;
                if (DateTime.TryParseExact(omieNf.Ide.DataEmissao, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dataParsed))
                {
                    dataEmissao = DateTime.SpecifyKind(dataParsed, DateTimeKind.Utc);
                }

                string status = omieNf.Ide.Situacao switch
                {
                    "101" => "CANCELADA",
                    "110" => "DENEGADA",
                    "301" => "DENEGADA",
                    _ => omieNf.Ide.Denegada == "S" ? "DENEGADA" : "AUTORIZADA"
                };

                TimeSpan? horaEmissao = null;
                if (TimeSpan.TryParse(omieNf.Ide.HoraEmissao, out TimeSpan horaParsed)) horaEmissao = horaParsed;

                if (existing == null)
                {
                    existing = new NotaFiscal
                    {
                        Id = Guid.NewGuid(),
                        OmieId = omieId,
                        NumeroNf = omieNf.Ide.Numero,
                        ChaveAcesso = omieNf.Compl.ChaveNfe,
                        Status = status,
                        CodigoStatus = int.TryParse(omieNf.Ide.Situacao, out int cs) ? cs : 0,
                        DataEmissao = dataEmissao,
                        HoraEmissao = horaEmissao,
                        NaturezaOperacao = omieNf.Compl.XNatureza,
                        Serie = omieNf.Ide.Serie,
                        Modelo = omieNf.Ide.Modelo,
                        TipoOperacao = omieNf.Ide.TipoNf,
                        Finalidade = omieNf.Ide.Finalidade,
                        Ambiente = omieNf.Ide.Ambiente,
                        InformacoesComplementares = omieNf.Compl.InformacoesComplementares,
                        InformacoesFisco = omieNf.Compl.InformacoesFisco,
                        ImportadoApi = true,
                        ValorTotal = omieNf.Total.IcmsTot.ValorNota,
                        ValorFrete = omieNf.Total.IcmsTot.ValorFrete,
                        ValorSeguro = omieNf.Total.IcmsTot.ValorSeguro,
                        ValorDesconto = omieNf.Total.IcmsTot.ValorDesconto,
                        ValorOutrasDespesas = omieNf.Total.IcmsTot.ValorOutrasDespesas,
                        IssqnBaseCalculo = omieNf.Total.IssqnTot?.BaseCalculo ?? 0,
                        ValorIss = omieNf.Total.IssqnTot?.ValorIss ?? 0,
                        ValorIr = omieNf.Total.RetTrib?.ValorIrrf ?? 0,
                        ValorCsll = omieNf.Total.RetTrib?.ValorCsll ?? 0,
                        ValorPisRetido = omieNf.Total.RetTrib?.ValorPis ?? 0,
                        ValorCofinsRetido = omieNf.Total.RetTrib?.ValorCofins ?? 0,
                        ValorIpi = omieNf.Total.IcmsTot.ValorIpi,
                        ValorPis = omieNf.Total.IcmsTot.ValorPis,
                        ValorCofins = omieNf.Total.IcmsTot.ValorCofins,
                        ValorProd = omieNf.Total.IcmsTot.ValorProdutos,
                        IcmsBaseCalculo = omieNf.Total.IcmsTot.BaseCalculoIcms,
                        IcmsValor = omieNf.Total.IcmsTot.ValorIcms,
                        ValorIbs = omieNf.Total.IcmsTot.ValorIbs,
                        ValorCbs = omieNf.Total.IcmsTot.ValorCbs,
                        Denegada = omieNf.Ide.Denegada == "S",
                        ClienteId = cliente.Id,
                        PedidoVendaId = pedido?.Id,
                        VendedorId = pedido?.VendedorId,
                        ContaCorrenteId = pedido?.ContaCorrenteId,
                        IdTransportadora = omieNf.Compl.IdTransportadora,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Itens = new List<ItemNotaFiscal>(),
                        Titulos = new List<NotaFiscalTitulo>()
                    };

                    if (DateTime.TryParseExact(omieNf.Ide.DataSaida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dSai)) existing.DataSaida = DateTime.SpecifyKind(dSai, DateTimeKind.Utc);
                    if (TimeSpan.TryParse(omieNf.Ide.HoraSaida, out var hSai)) existing.HoraSaida = hSai;

                    dbContext.NotasFiscais.Add(existing);
                }
                else
                {
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieNf.Info?.DAlt, omieNf.Info?.HAlt);
                    if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue && existing.OmieUpdatedAt.Value == omieLastAlt.Value) continue;

                    existing.Status = status;
                    existing.CodigoStatus = int.TryParse(omieNf.Ide.Situacao, out int csUpd) ? csUpd : 0;
                    existing.ChaveAcesso = omieNf.Compl.ChaveNfe;
                    existing.HoraEmissao = horaEmissao;
                    existing.ValorIss = omieNf.Total.IssqnTot?.ValorIss ?? 0;
                    existing.ValorIr = omieNf.Total.RetTrib?.ValorIrrf ?? 0;
                    existing.ValorCsll = omieNf.Total.RetTrib?.ValorCsll ?? 0;
                    existing.ValorPisRetido = omieNf.Total.RetTrib?.ValorPis ?? 0;
                    existing.ValorCofinsRetido = omieNf.Total.RetTrib?.ValorCofins ?? 0;
                    existing.NaturezaOperacao = omieNf.Compl.XNatureza;
                    existing.Serie = omieNf.Ide.Serie;
                    existing.Modelo = omieNf.Ide.Modelo;
                    existing.TipoOperacao = omieNf.Ide.TipoNf;
                    existing.Finalidade = omieNf.Ide.Finalidade;
                    existing.Ambiente = omieNf.Ide.Ambiente;
                    existing.InformacoesComplementares = omieNf.Compl.InformacoesComplementares;
                    existing.InformacoesFisco = omieNf.Compl.InformacoesFisco;
                    existing.ValorFrete = omieNf.Total.IcmsTot.ValorFrete;
                    existing.ValorSeguro = omieNf.Total.IcmsTot.ValorSeguro;
                    existing.ValorDesconto = omieNf.Total.IcmsTot.ValorDesconto;
                    existing.ValorOutrasDespesas = omieNf.Total.IcmsTot.ValorOutrasDespesas;
                    existing.IssqnBaseCalculo = omieNf.Total.IssqnTot?.BaseCalculo ?? 0;
                    existing.ValorIpi = omieNf.Total.IcmsTot.ValorIpi;
                    existing.ValorPis = omieNf.Total.IcmsTot.ValorPis;
                    existing.ValorCofins = omieNf.Total.IcmsTot.ValorCofins;
                    existing.ValorProd = omieNf.Total.IcmsTot.ValorProdutos;
                    existing.IcmsBaseCalculo = omieNf.Total.IcmsTot.BaseCalculoIcms;
                    existing.IcmsValor = omieNf.Total.IcmsTot.ValorIcms;
                    existing.ValorIbs = omieNf.Total.IcmsTot.ValorIbs;
                    existing.ValorCbs = omieNf.Total.IcmsTot.ValorCbs;
                    existing.Denegada = omieNf.Ide.Denegada == "S";
                    existing.VendedorId = pedido?.VendedorId;
                    existing.ContaCorrenteId = pedido?.ContaCorrenteId;
                    existing.IdTransportadora = omieNf.Compl.IdTransportadora;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.OmieUpdatedAt = omieLastAlt;

                    if (DateTime.TryParseExact(omieNf.Ide.DataSaida, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dSaiU)) existing.DataSaida = DateTime.SpecifyKind(dSaiU, DateTimeKind.Utc);
                    else existing.DataSaida = null;
                    if (TimeSpan.TryParse(omieNf.Ide.HoraSaida, out var hSaiU)) existing.HoraSaida = hSaiU;
                    else existing.HoraSaida = null;

                    foreach (var item in existing.Itens.ToList()) dbContext.ItensNotaFiscal.Remove(item);
                    existing.Itens.Clear();
                    foreach (var titulo in existing.Titulos.ToList()) dbContext.NotaFiscalTitulos.Remove(titulo);
                    existing.Titulos.Clear();
                }

                foreach (var det in omieNf.Det)
                {
                    if (produtos.TryGetValue(det.Prod.Codigo, out var produto))
                    {
                        existing.Itens.Add(new ItemNotaFiscal
                        {
                            Id = Guid.NewGuid(),
                            NotaFiscalId = existing.Id,
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

                if (omieNf.Titulos != null)
                {
                    foreach (var tit in omieNf.Titulos)
                    {
                        if (DateTime.TryParseExact(tit.DataVencimento, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtVenc))
                        {
                            vendedores.TryGetValue(tit.CodigoVendedor, out var titVendedor);
                            existing.Titulos.Add(new NotaFiscalTitulo
                            {
                                Id = Guid.NewGuid(),
                                NotaFiscalId = existing.Id,
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

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                foreach (var entry in dbContext.ChangeTracker.Entries().ToList()) entry.State = EntityState.Detached;
            }
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
