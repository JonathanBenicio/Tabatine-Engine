using Tabatine.Core.Interfaces;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models.Financeiro;
using System.Globalization;

namespace Tabatine.Infrastructure.Services;

public class ContasPagarSyncService(
    IOmieClient omieClient,
    AppDbContext dbContext,
    ISyncStateRepository syncState,
    ILogger<ContasPagarSyncService> logger) : ISyncService
{
    private const string SyncKey = "LancamentosPagar";

    // Chave legada antes do rename no PR #47 — mantida para migração
    // transparente do cursor na primeira execução após o deploy.
    private const string SyncKeyLegado = "ContasPagar";

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Iniciando sincronização de Contas a Pagar...");

        // Se não existe cursor com a nova chave, reaproveitamos o cursor legado
        // para evitar full sync desnecessário no primeiro deploy após o rename.
        var lastSyncDate = await syncState.GetLastSyncDateAsync(SyncKey, ct);
        if (lastSyncDate == null)
        {
            var legacyDate = await syncState.GetLastSyncDateAsync(SyncKeyLegado, ct);
            if (legacyDate != null)
            {
                logger.LogInformation(
                    "Cursor legado 'ContasPagar' encontrado ({Date}). Migrando para 'LancamentosPagar'.",
                    legacyDate);
                lastSyncDate = legacyDate;
            }
        }

        var syncStartTime = DateTime.UtcNow;
        var processedOmieIds = new HashSet<long>();
        int pagina = 1;
        bool temMais = true;

        while (temMais && !ct.IsCancellationRequested)
        {
            // filtrar_por_vendedor removido — a Omie retorna 500 neste endpoint com esse filtro
            var response = await omieClient.ListarContasPagarAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

            if (response?.ContasPagar == null || response.ContasPagar.Count == 0) break;

            var omieClienteIds = response.ContasPagar
                .Where(t => t.CodigoClienteFornecedor > 0)
                .Select(t => t.CodigoClienteFornecedor).Distinct().ToList();

            var clientes = await dbContext.Clientes
                .Where(c => omieClienteIds.Contains(c.OmieId))
                .ToDictionaryAsync(c => c.OmieId, ct);

            var omieContaIds = response.ContasPagar
                .Where(t => t.CodigoContaCorrente.HasValue)
                .Select(t => t.CodigoContaCorrente!.Value).Distinct().ToList();

            var contasCorrente = await dbContext.ContasCorrente
                .Where(cc => omieContaIds.Contains(cc.OmieId))
                .ToDictionaryAsync(cc => cc.OmieId, ct);

            var omieIds = response.ContasPagar.Select(t => t.CodigoLancamentoOmie).ToList();
            var existingTitulos = await dbContext.TitulosPagar
                .Where(t => omieIds.Contains(t.OmieId))
                .ToDictionaryAsync(t => t.OmieId, ct);

            foreach (var omieItem in response.ContasPagar)
            {
                var omieId = omieItem.CodigoLancamentoOmie;

                if (!processedOmieIds.Add(omieId))
                {
                    logger.LogDebug("TituloPagar OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
                    continue;
                }

                clientes.TryGetValue(omieItem.CodigoClienteFornecedor, out var cliente);
                contasCorrente.TryGetValue(omieItem.CodigoContaCorrente ?? 0, out var contaCorrente);
                existingTitulos.TryGetValue(omieId, out var existing);

                if (existing == null)
                {
                    var novo = MapToEntity(omieItem, cliente, contaCorrente);
                    dbContext.TitulosPagar.Add(novo);
                    existingTitulos[omieId] = novo;
                }
                else
                {
                    AtualizarEntidade(existing, omieItem, cliente, contaCorrente);
                }
            }

            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Página {Pagina} de {Total} de Contas a Pagar sincronizada.", pagina, response.TotalDePaginas);

            temMais = pagina < response.TotalDePaginas;
            pagina++;
        }

        if (!ct.IsCancellationRequested)
        {
            await syncState.SetLastSyncDateAsync(SyncKey, syncStartTime, ct);
        }

        logger.LogInformation("Sincronização de Contas a Pagar finalizada. Total processado: {Count}", processedOmieIds.Count);
    }

    public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
    {
        logger.LogInformation("Sincronizando TituloPagar específico OmieId: {OmieId}", omieId);
        var omieItem = await omieClient.ConsultarContaPagarAsync(omieId, ct);

        if (omieItem != null)
        {
            var cliente = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieItem.CodigoClienteFornecedor, ct);
            var contaCorrente = omieItem.CodigoContaCorrente.HasValue
                ? await dbContext.ContasCorrente.FirstOrDefaultAsync(cc => cc.OmieId == omieItem.CodigoContaCorrente.Value, ct)
                : null;

            var existing = await dbContext.TitulosPagar.FirstOrDefaultAsync(t => t.OmieId == omieId, ct);

            if (existing == null)
                dbContext.TitulosPagar.Add(MapToEntity(omieItem, cliente, contaCorrente));
            else
                AtualizarEntidade(existing, omieItem, cliente, contaCorrente);

            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("TituloPagar OmieId {OmieId} sincronizado com sucesso.", omieId);
        }
        else
        {
            logger.LogWarning("TituloPagar OmieId {OmieId} não encontrado na Omie.", omieId);
        }
    }

    public async Task CancelByIdAsync(long omieId, CancellationToken ct = default)
    {
        logger.LogInformation("Cancelando TituloPagar OmieId: {OmieId} via soft-delete.", omieId);
        var existing = await dbContext.TitulosPagar.FirstOrDefaultAsync(t => t.OmieId == omieId, ct);

        if (existing != null)
        {
            existing.StatusTitulo = "CANCELADO";
            existing.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("TituloPagar OmieId {OmieId} marcado como CANCELADO.", omieId);
        }
    }

    private static TituloPagar MapToEntity(OmieContaPagar omie, Cliente? cliente, ContaCorrente? contaCorrente) =>
        new()
        {
            Id = Guid.NewGuid(),
            OmieId = omie.CodigoLancamentoOmie,
            NumeroDocumento = omie.NumeroDocumento ?? string.Empty,
            NumeroPedido = omie.NumeroPedido,
            DataEmissao = ParseDataOptional(omie.DataEmissao) ?? DateTime.UtcNow,
            DataVencimento = ParseData(omie.DataVencimento),
            DataBaixa = ParseDataOptional(omie.DataBaixa),
            ValorDocumento = omie.ValorDocumento,
            ValorPago = omie.ValorPago,
            ValorSaldo = omie.ValorSaldo,
            StatusTitulo = omie.StatusTitulo,
            CodigoCategoria = omie.CodigoCategoria,
            Observacao = omie.Observacao,
            ClienteId = cliente?.Id,
            ContaCorrenteId = contaCorrente?.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static void AtualizarEntidade(TituloPagar existing, OmieContaPagar omie, Cliente? cliente, ContaCorrente? contaCorrente)
    {
        existing.NumeroDocumento = omie.NumeroDocumento ?? string.Empty;
        existing.NumeroPedido = omie.NumeroPedido;
        existing.DataEmissao = ParseDataOptional(omie.DataEmissao) ?? existing.DataEmissao;
        existing.DataVencimento = ParseData(omie.DataVencimento);
        existing.DataBaixa = ParseDataOptional(omie.DataBaixa);
        existing.ValorDocumento = omie.ValorDocumento;
        existing.ValorPago = omie.ValorPago;
        existing.ValorSaldo = omie.ValorSaldo;
        existing.StatusTitulo = omie.StatusTitulo;
        existing.CodigoCategoria = omie.CodigoCategoria;
        existing.Observacao = omie.Observacao;
        existing.ClienteId = cliente?.Id;
        existing.ContaCorrenteId = contaCorrente?.Id;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    // Omie retorna datas no formato dd/MM/yyyy
    private static DateTime ParseData(string data) =>
        DateTime.ParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    private static DateTime? ParseDataOptional(string? data) =>
        string.IsNullOrWhiteSpace(data) ? null : ParseData(data);
}
