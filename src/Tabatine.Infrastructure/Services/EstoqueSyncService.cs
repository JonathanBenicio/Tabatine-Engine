using Tabatine.Core.Interfaces;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models.Estoque;

namespace Tabatine.Infrastructure.Services;

public class EstoqueSyncService(
    IOmieClient omieClient,
    AppDbContext dbContext,
    ISyncStateRepository syncState,
    ILogger<EstoqueSyncService> logger) : ISyncService
{
    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Iniciando sincronização completa de Estoque...");
        var syncStartTime = DateTime.UtcNow;

        await SyncLocaisAsync(ct);
        await SyncSaldosAsync(ct);

        if (!ct.IsCancellationRequested)
        {
            await syncState.SetLastSyncDateAsync("Estoque", syncStartTime, ct);
        }

        logger.LogInformation("Sincronização de Estoque finalizada.");
    }

    public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
    {
        await SyncProdutoSaldoAsync(omieId, ct);
    }

    public async Task SyncLocalByIdAsync(long localId, CancellationToken ct = default)
    {
        logger.LogInformation("Sincronizando Local de Estoque individual. OmieId: {LocalId}", localId);
        await SyncLocaisAsync(ct);
    }

    private async Task SyncProdutoSaldoAsync(long omieId, CancellationToken ct)
    {
        logger.LogInformation("Sincronizando saldo específico para Produto OmieId: {OmieId}", omieId);

        var produto = await dbContext.Produtos.FirstOrDefaultAsync(p => p.OmieId == omieId, ct);
        if (produto == null)
        {
            logger.LogWarning("Produto OmieId {OmieId} não encontrado no banco local. Sincronize o produto primeiro.", omieId);
            return;
        }

        var request = new ObterEstoqueProdutoRequest { IdProduto = omieId };
        var response = await omieClient.ObterResumoEstoqueProdutoAsync(request, ct);

        if (response?.ListaEstoque == null) return;

        var locaisCache = await dbContext.LocaisEstoque.ToDictionaryAsync(l => l.OmieId, ct);

        foreach (var item in response.ListaEstoque)
        {
            if (!locaisCache.TryGetValue(item.IdLocal, out var local))
            {
                logger.LogWarning("Local de Estoque OmieId {LocalId} não encontrado localmente. Sincronize locais primeiro.", item.IdLocal);
                continue;
            }

            await UpsertSaldoAsync(produto.Id, local.Id, new ProdutoEstoqueDto
            {
                Saldo = item.Disponivel,
                Reservado = item.Reservado,
                Pendente = item.PrevisaoSaida,
                Fisico = item.Fisico,
                Cmc = item.Cmc,
                EstoqueMinimo = item.EstoqueMinimo
            }, ct);
        }

        await SaveChangesSafelyAsync(ct);
    }

    private async Task SyncLocaisAsync(CancellationToken ct)
    {
        logger.LogInformation("Sincronizando Locais de Estoque...");

        var existingLocais = await dbContext.LocaisEstoque.ToDictionaryAsync(l => l.OmieId, ct);

        await foreach (var item in omieClient.ListarLocaisEstoqueAsync(ct))
        {
            if (existingLocais.TryGetValue(item.CodigoLocalEstoque, out var existing))
            {
                existing.Codigo = item.Codigo ?? existing.Codigo;
                existing.Descricao = item.Descricao ?? existing.Descricao;
                existing.Tipo = item.Tipo;
                existing.Padrao = item.Padrao == "S";
                existing.Inativo = item.Inativo == "S";
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var novo = new LocalEstoque
                {
                    Id = Guid.NewGuid(),
                    OmieId = item.CodigoLocalEstoque,
                    Codigo = item.Codigo ?? string.Empty,
                    Descricao = item.Descricao ?? string.Empty,
                    Tipo = item.Tipo,
                    Padrao = item.Padrao == "S",
                    Inativo = item.Inativo == "S",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.LocaisEstoque.Add(novo);
                existingLocais[novo.OmieId] = novo;
            }
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Sincronização de Locais de Estoque finalizada.");
    }

    private async Task SyncSaldosAsync(CancellationToken ct)
    {
        logger.LogInformation("Sincronizando Saldos de Estoque (Streaming)...");

        var produtosCache = await dbContext.Produtos
            .Select(p => new { p.Id, p.OmieId })
            .ToDictionaryAsync(p => p.OmieId, p => p.Id, ct);

        var locaisCache = await dbContext.LocaisEstoque
            .Select(l => new { l.Id, l.OmieId })
            .ToDictionaryAsync(l => l.OmieId, l => l.Id, ct);

        var request = new ListarPosEstoqueRequest { Pagina = 1, RegPorPagina = 100, ExibeTodos = "S" };

        int count = 0;
        List<(Guid produtoId, Guid localId, ProdutoEstoqueDto dto)> pendingSaldos = [];

        await foreach (var item in omieClient.StreamPosicaoEstoqueAsync(request, ct))
        {
            if (!produtosCache.TryGetValue(item.CodProd, out var produtoId))
            {
                logger.LogDebug("Produto OmieId {OmieId} não encontrado no banco local. Pulando saldo.", item.CodProd);
                continue;
            }

            if (!locaisCache.TryGetValue(item.CodigoLocalEstoque, out var localId))
            {
                logger.LogWarning("Local OmieId {LocalId} não encontrado localmente. Pulando saldo para Produto {CodProd}.",
                    item.CodigoLocalEstoque, item.CodProd);
                continue;
            }

            pendingSaldos.Add((produtoId, localId, item));
            count++;

            if (count % 500 == 0)
            {
                foreach (var (pId, lId, dto) in pendingSaldos)
                    await UpsertSaldoAsync(pId, lId, dto, ct);

                await SaveChangesSafelyAsync(ct);
                logger.LogInformation("Progresso do Estoque: {Count} saldos processados.", count);
                pendingSaldos.Clear();
            }
        }

        foreach (var (pId, lId, dto) in pendingSaldos)
            await UpsertSaldoAsync(pId, lId, dto, ct);

        await SaveChangesSafelyAsync(ct);
        logger.LogInformation("Total de {Count} registros de saldo processados.", count);
    }

    // Se o SaveChanges em lote falhar por FK inválida (23503), tenta cada entidade
    // individualmente para descartar apenas o(s) registro(s) problemático(s), preservando o restante.
    private async Task SaveChangesSafelyAsync(CancellationToken ct)
    {
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23503" })
        {
            logger.LogWarning(ex, "Violação de FK no lote de estoque. Tentando salvar item por item para isolar o problema.");

            var entries = dbContext.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .ToList();

            dbContext.ChangeTracker.Clear();

            int salvos = 0;
            int descartados = 0;

            foreach (var entry in entries)
            {
                try
                {
                    dbContext.Entry(entry.Entity).State = entry.State;
                    await dbContext.SaveChangesAsync(ct);
                    salvos++;
                }
                catch (DbUpdateException itemEx) when (itemEx.InnerException is Npgsql.PostgresException { SqlState: "23503" })
                {
                    logger.LogWarning("Saldo descartado por FK inválida: {Entity}.", entry.Entity.GetType().Name);
                    dbContext.ChangeTracker.Clear();
                    descartados++;
                }
            }

            logger.LogInformation(
                "Salvamento item-por-item concluído: {Salvos} salvos, {Descartados} descartados por FK inválida.",
                salvos, descartados);
        }
    }

    private async Task UpsertSaldoAsync(Guid produtoId, Guid localId, ProdutoEstoqueDto dto, CancellationToken ct)
    {
        var existing = await dbContext.ProdutosEstoque
            .FirstOrDefaultAsync(se => se.ProdutoId == produtoId && se.LocalEstoqueId == localId, ct);

        if (existing != null)
        {
            existing.Saldo = dto.Saldo;
            existing.Reservado = dto.Reservado;
            existing.Pendente = dto.Pendente;
            existing.Fisico = dto.Fisico;
            existing.Cmc = dto.Cmc;
            existing.EstoqueMinimo = dto.EstoqueMinimo;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            dbContext.ProdutosEstoque.Add(new ProdutoEstoque
            {
                Id = Guid.NewGuid(),
                ProdutoId = produtoId,
                LocalEstoqueId = localId,
                Saldo = dto.Saldo,
                Reservado = dto.Reservado,
                Pendente = dto.Pendente,
                Fisico = dto.Fisico,
                Cmc = dto.Cmc,
                EstoqueMinimo = dto.EstoqueMinimo,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
}
