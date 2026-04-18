using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class ProdutoSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState, 
        ILogger<ProdutoSyncService> logger,
        IDistributedLockService lockService) : ISyncService
    {
        private const string EntityName = "produto";
        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Produtos...");

            var lastSyncDate = await syncState.GetLastSyncDateAsync("Produtos", ct);
            var syncStartTime = DateTime.UtcNow;

            // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
            var processedOmieIds = new HashSet<long>();

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarProdutosAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                // Resposta nula = sem registros (Client-5113)
                if (response == null || response.ProdutosCadastro == null || response.ProdutosCadastro.Count == 0) break;

                var omieIds = response.ProdutosCadastro.Select(p => p.CodigoProduto).ToList();

                var existingProdutos = await dbContext.Produtos
                    .Where(p => omieIds.Contains(p.OmieId))
                    .ToDictionaryAsync(p => p.OmieId, ct);

                var activeLocks = new Dictionary<long, string>();

                try 
                {
                    foreach (var omieItem in response.ProdutosCadastro)
                    {
                        var omieId = omieItem.CodigoProduto;

                        // Adquire trava individual
                        var lockToken = await AcquireResourceLockAsync(omieId, ct);
                        if (lockToken == null)
                        {
                            logger.LogWarning("Não foi possível adquirir trava para o Produto {OmieId} em lote. Pulando.", omieId);
                            continue;
                        }
                        activeLocks[omieId] = lockToken;

                        // Pula se já processamos este OmieId neste ciclo (e já temos a trava)
                        if (!processedOmieIds.Add(omieId))
                        {
                            logger.LogDebug("Produto OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
                            continue;
                        }

                        existingProdutos.TryGetValue(omieId, out var existing);
                        var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieItem.DAlt, omieItem.HAlt);

                        if (existing == null)
                        {
                            var novoProduto = new Produto
                            {
                                Id = Guid.NewGuid(),
                                OmieId = omieId,
                                CodigoProduto = omieItem.Codigo,
                                Descricao = omieItem.Descricao,
                                PrecoUnitario = omieItem.ValorUnitario,
                                Ncm = omieItem.Ncm,
                                UnidadeMedida = omieItem.Unidade,
                                PesoLiquido = omieItem.PesoLiquido,
                                PesoBruto = omieItem.PesoBruto,
                                FamiliaProduto = omieItem.FamiliaProduto,
                                Ativo = omieItem.Inativo == "N",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                OmieUpdatedAt = omieLastAlt
                            };
                            dbContext.Produtos.Add(novoProduto);
                            existingProdutos[omieId] = novoProduto;
                        }
                        else
                        {
                            if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                                existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                            {
                                continue;
                            }

                            existing.CodigoProduto = omieItem.Codigo;
                            existing.Descricao = omieItem.Descricao;
                            existing.PrecoUnitario = omieItem.ValorUnitario;
                            existing.Ncm = omieItem.Ncm;
                            existing.UnidadeMedida = omieItem.Unidade;
                            existing.PesoLiquido = omieItem.PesoLiquido;
                            existing.PesoBruto = omieItem.PesoBruto;
                            existing.FamiliaProduto = omieItem.FamiliaProduto;
                            existing.Ativo = omieItem.Inativo == "N";
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

                logger.LogInformation("Página {Pagina} de {Total} de produtos sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("Produtos", syncStartTime, ct);
            }

            logger.LogInformation("Sincronização de Produtos finalizada.");
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            var lockToken = await AcquireResourceLockAsync(omieId, ct);
            if (lockToken == null)
            {
                logger.LogWarning("Não foi possível adquirir trava para o Produto {OmieId} após espera. Abortando sync individual.", omieId);
                return;
            }

            try 
            {
                logger.LogInformation("Sincronizando Produto específico OmieId: {OmieId}", omieId);
                var omieProduto = await omieClient.ConsultarProdutoAsync(omieId, ct);

                if (omieProduto != null)
                {
                    var existing = await dbContext.Produtos.FirstOrDefaultAsync(p => p.OmieId == omieId, ct);
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieProduto.DAlt, omieProduto.HAlt);

                    if (existing == null)
                    {
                        var novoProduto = new Produto
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieId,
                            CodigoProduto = omieProduto.Codigo,
                            Descricao = omieProduto.Descricao,
                            PrecoUnitario = omieProduto.ValorUnitario,
                            Ncm = omieProduto.Ncm,
                            UnidadeMedida = omieProduto.Unidade,
                            PesoLiquido = omieProduto.PesoLiquido,
                            PesoBruto = omieProduto.PesoBruto,
                            FamiliaProduto = omieProduto.FamiliaProduto,
                            Ativo = omieProduto.Inativo == "N",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            OmieUpdatedAt = omieLastAlt
                        };
                        dbContext.Produtos.Add(novoProduto);
                    }
                    else
                    {
                        if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                            existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                        {
                            return;
                        }

                        existing.CodigoProduto = omieProduto.Codigo;
                        existing.Descricao = omieProduto.Descricao;
                        existing.PrecoUnitario = omieProduto.ValorUnitario;
                        existing.Ncm = omieProduto.Ncm;
                        existing.UnidadeMedida = omieProduto.Unidade;
                        existing.PesoLiquido = omieProduto.PesoLiquido;
                        existing.PesoBruto = omieProduto.PesoBruto;
                        existing.FamiliaProduto = omieProduto.FamiliaProduto;
                        existing.Ativo = omieProduto.Inativo == "N";
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

                    logger.LogInformation("Produto OmieId {OmieId} sincronizado individualmente com sucesso.", omieId);
                }
                else
                {
                    logger.LogWarning("Produto OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
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
