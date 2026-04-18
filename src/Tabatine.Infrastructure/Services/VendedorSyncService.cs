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
using Tabatine.Omie.Client.Models;


namespace Tabatine.Infrastructure.Services
{
    public class VendedorSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState,
        ILogger<VendedorSyncService> logger,
        IDistributedLockService lockService) : ISyncService
    {
        private const string EntityName = "vendedor";
        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            var lastSyncDate = await syncState.GetLastSyncDateAsync("Vendedores", ct);
            logger.LogInformation("Iniciando sincronização de Vendedores a partir de {LastSyncDate}...", lastSyncDate?.ToString("yyyy-MM-dd") ?? "o início");

            // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
            var processedOmieIds = new HashSet<long>();

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarVendedoresAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                if (response == null || response.Vendedores == null || response.Vendedores.Count == 0) break;

                var omieIds = response.Vendedores.Select(v => v.Codigo).ToList();
                var existingVendedores = await dbContext.Vendedores
                    .Where(v => omieIds.Contains(v.OmieId))
                    .ToDictionaryAsync(v => v.OmieId, ct);

                var activeLocks = new Dictionary<long, string>();

                try 
                {
                    foreach (var omieItem in response.Vendedores)
                    {
                        var omieId = omieItem.Codigo;

                        // Adquire trava individual
                        var lockToken = await AcquireResourceLockAsync(omieId, ct);
                        if (lockToken == null)
                        {
                            logger.LogWarning("Não foi possível adquirir trava para o Vendedor {OmieId} em lote. Pulando.", omieId);
                            continue;
                        }
                        activeLocks[omieId] = lockToken;

                        // Pula se já processamos este OmieId neste ciclo
                        if (!processedOmieIds.Add(omieId))
                        {
                            logger.LogDebug("Vendedor OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
                            continue;
                        }

                        existingVendedores.TryGetValue(omieId, out var existing);
                        var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieItem.DAlt, omieItem.HAlt);

                        if (existing == null)
                        {
                            var novoVendedor = new Vendedor
                            {
                                Id = Guid.NewGuid(),
                                OmieId = omieId,
                                Nome = omieItem.Nome,
                                Email = omieItem.Email,
                                Comissao = omieItem.Comissao,
                                Inativo = omieItem.Inativo == "S",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                OmieUpdatedAt = omieLastAlt
                            };
                            dbContext.Vendedores.Add(novoVendedor);
                            existingVendedores[omieId] = novoVendedor;
                        }
                        else
                        {
                            // Se o timestamp da Omie for igual ao que já temos, pula o update
                            if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                                existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                            {
                                continue;
                            }

                            existing.Nome = omieItem.Nome;
                            existing.Email = omieItem.Email;
                            existing.Comissao = omieItem.Comissao;
                            existing.Inativo = omieItem.Inativo == "S";
                            existing.UpdatedAt = DateTime.UtcNow;
                            existing.OmieUpdatedAt = omieLastAlt;
                        }
                    }

                    int retryCount = 0;
                    const int maxRetries = 3;
                    while (true)
                    {
                        try
                        {
                            await dbContext.SaveChangesAsync(ct);
                            break;
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            retryCount++;
                            if (retryCount > maxRetries)
                            {
                                logger.LogError(ex, "Erro de concorrência persistente em {Entity} (Lote) após {Count} tentativas.", EntityName, retryCount);
                                throw;
                            }

                            var delay = Random.Shared.Next(100, 500);
                            logger.LogWarning(ex, "Concorrência detectada em {Entity} (Lote). Tentativa {Retry} de {MaxRetries}. Aguardando {Delay}ms.", 
                                EntityName, retryCount, maxRetries, delay);

                            await Task.Delay(delay, ct);

                            foreach (var entry in ex.Entries)
                            {
                                var dbVals = await entry.GetDatabaseValuesAsync(ct);
                                if (dbVals == null) entry.State = EntityState.Detached;
                                else entry.OriginalValues.SetValues(dbVals);
                            }
                        }
                    }
                }
                finally 
                {
                    foreach (var kvp in activeLocks)
                        await lockService.ReleaseLockAsync(GetLockKey(kvp.Key), kvp.Value, ct);
                }

                logger.LogInformation("Página {Pagina} de {Total} de vendedores sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("Vendedores", DateTime.UtcNow, ct);
            }

            logger.LogInformation("Sincronização de Vendedores finalizada.");
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            var lockToken = await AcquireResourceLockAsync(omieId, ct);
            if (lockToken == null)
            {
                logger.LogWarning("Não foi possível adquirir trava para o Vendedor {OmieId} após espera. Abortando sync individual.", omieId);
                return;
            }

            try 
            {
                logger.LogInformation("Sincronizando Vendedor específico OmieId: {OmieId}", omieId);
                var omieVendedor = await omieClient.ConsultarVendedorAsync(omieId, ct);

                if (omieVendedor != null)
                {
                    var existing = await dbContext.Vendedores.FirstOrDefaultAsync(v => v.OmieId == omieId, ct);
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieVendedor.DAlt, omieVendedor.HAlt);

                    if (existing == null)
                    {
                        var novoVendedor = new Vendedor
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieId,
                            Nome = omieVendedor.Nome,
                            Email = omieVendedor.Email,
                            Comissao = omieVendedor.Comissao,
                            Inativo = omieVendedor.Inativo == "S",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            OmieUpdatedAt = omieLastAlt
                        };
                        dbContext.Vendedores.Add(novoVendedor);
                    }
                    else
                    {
                        if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                            existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                        {
                            return;
                        }

                        existing.Nome = omieVendedor.Nome;
                        existing.Email = omieVendedor.Email;
                        existing.Comissao = omieVendedor.Comissao;
                        existing.Inativo = omieVendedor.Inativo == "S";
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.OmieUpdatedAt = omieLastAlt;
                    }

                    int retryCount = 0;
                    const int maxRetries = 3;
                    while (true)
                    {
                        try
                        {
                            await dbContext.SaveChangesAsync(ct);
                            break;
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            retryCount++;
                            if (retryCount > maxRetries)
                            {
                                logger.LogError(ex, "Erro de concorrência persistente em {Entity} individual ({OmieId}) após {Count} tentativas.", EntityName, omieId, retryCount);
                                throw;
                            }

                            var delay = Random.Shared.Next(100, 500);
                            logger.LogWarning(ex, "Concorrência detectada em {Entity} individual ({OmieId}). Tentativa {Retry} de {MaxRetries}. Aguardando {Delay}ms.", 
                                EntityName, omieId, retryCount, maxRetries, delay);

                            await Task.Delay(delay, ct);

                            foreach (var entry in ex.Entries)
                            {
                                var dbVals = await entry.GetDatabaseValuesAsync(ct);
                                if (dbVals == null) entry.State = EntityState.Detached;
                                else entry.OriginalValues.SetValues(dbVals);
                            }
                        }
                    }

                    logger.LogInformation("Vendedor OmieId {OmieId} sincronizado individualmente com sucesso.", omieId);
                }
                else
                {
                    logger.LogWarning("Vendedor OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
                }
            }
            finally 
            {
                await lockService.ReleaseLockAsync(GetLockKey(omieId), lockToken, ct);
            }
        }

        private async Task<string?> AcquireResourceLockAsync(long omieId, CancellationToken ct)
        {
            var lockKey = GetLockKey(omieId);
            var lockToken = Guid.NewGuid().ToString();
            for (int i = 0; i < 60; i++) 
            {
                if (await lockService.TryAcquireLockAsync(lockKey, lockToken, TimeSpan.FromMinutes(2), ct))
                    return lockToken;
                
                await Task.Delay(500, ct); 
            }
            return null;
        }

        private string GetLockKey(long omieId) => $"sync:{EntityName}:{omieId}";

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
