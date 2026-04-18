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
    public class ClienteSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState, 
        ILogger<ClienteSyncService> logger,
        IDistributedLockService lockService) : ISyncService
    {
        private const string EntityName = "cliente";
        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Clientes...");

            var lastSyncDate = await syncState.GetLastSyncDateAsync("Clientes", ct);
            var syncStartTime = DateTime.UtcNow;

            var processedOmieIds = new HashSet<long>();
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarClientesAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                if (response == null || response.ClientesCadastro == null || response.ClientesCadastro.Count == 0) break;

                var omieIds = response.ClientesCadastro.Select(c => c.CodigoClienteOmie).ToList();
                var existingClientes = await dbContext.Clientes
                    .Where(c => omieIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                var activeLocks = new Dictionary<long, string>();

                try 
                {
                    foreach (var omieCliente in response.ClientesCadastro)
                    {
                        var omieId = omieCliente.CodigoClienteOmie;

                        // Adquire trava sem espera no lote: pula registro imediatamente se já estiver sendo processado por outro worker
                        var lockToken = await AcquireResourceLockAsync(omieId, ct, wait: false);
                        if (lockToken == null)
                        {
                            logger.LogInformation("Cliente {OmieId} já está sendo processado por outro worker. Pulando no lote.", omieId);
                            continue;
                        }
                        activeLocks[omieId] = lockToken;

                        if (!processedOmieIds.Add(omieId))
                        {
                            logger.LogWarning("Cliente OmieId {OmieId} duplicado na resposta da Omie. Pulando.", omieId);
                            continue;
                        }

                        existingClientes.TryGetValue(omieId, out var existing);
                        var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieCliente.DAlt, omieCliente.HAlt);

                        if (existing == null)
                        {
                            var novo = new Cliente
                            {
                                Id = Guid.NewGuid(),
                                OmieId = omieId,
                                RazaoSocial = omieCliente.RazaoSocial,
                                NomeFantasia = omieCliente.NomeFantasia,
                                CnpjCpf = omieCliente.CnpjCpf,
                                Email = omieCliente.Email,
                                Telefone = omieCliente.Telefone,
                                Endereco = omieCliente.Endereco,
                                EnderecoNumero = omieCliente.EnderecoNumero,
                                EnderecoComplemento = omieCliente.Complemento,
                                Bairro = omieCliente.Bairro,
                                Cep = omieCliente.Cep,
                                Estado = omieCliente.Estado,
                                Cidade = omieCliente.Cidade,
                                InscricaoEstadual = omieCliente.InscricaoEstadual,
                                InscricaoMunicipal = omieCliente.InscricaoMunicipal,
                                OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                OmieUpdatedAt = omieLastAlt
                            };

                            dbContext.Clientes.Add(novo);
                            existingClientes[omieId] = novo;
                        }
                        else
                        {
                            if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                                existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                            {
                                continue;
                            }

                            existing.RazaoSocial = omieCliente.RazaoSocial;
                            existing.NomeFantasia = omieCliente.NomeFantasia;
                            existing.CnpjCpf = omieCliente.CnpjCpf;
                            existing.Email = omieCliente.Email;
                            existing.Telefone = omieCliente.Telefone;
                            existing.Endereco = omieCliente.Endereco;
                            existing.EnderecoNumero = omieCliente.EnderecoNumero;
                            existing.EnderecoComplemento = omieCliente.Complemento;
                            existing.Bairro = omieCliente.Bairro;
                            existing.Cep = omieCliente.Cep;
                            existing.Estado = omieCliente.Estado;
                            existing.Cidade = omieCliente.Cidade;
                            existing.InscricaoEstadual = omieCliente.InscricaoEstadual;
                            existing.InscricaoMunicipal = omieCliente.InscricaoMunicipal;
                            existing.OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S";
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

                logger.LogInformation("Página {Pagina} de {Total} de clientes sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("Clientes", syncStartTime, ct);
            }

            logger.LogInformation("Sincronização de Clientes finalizada.");
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            // Sync individual (webhook): aguarda até 30s para garantir que a notificação seja processada
            var lockToken = await AcquireResourceLockAsync(omieId, ct, wait: true);
            if (lockToken == null)
            {
                logger.LogWarning("Não foi possível adquirir trava para o Cliente {OmieId} após espera. Abortando sync individual.", omieId);
                return;
            }

            try 
            {
                logger.LogInformation("Sincronizando Cliente/Fornecedor específico OmieId: {OmieId}", omieId);
                var omieCliente = await omieClient.ConsultarClienteAsync(omieId, ct);

                if (omieCliente != null)
                {
                    var existing = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieId, ct);
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieCliente.DAlt, omieCliente.HAlt);

                    if (existing == null)
                    {
                        var novo = new Cliente
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieId,
                            RazaoSocial = omieCliente.RazaoSocial,
                            NomeFantasia = omieCliente.NomeFantasia,
                            CnpjCpf = omieCliente.CnpjCpf,
                            Email = omieCliente.Email,
                            Telefone = omieCliente.Telefone,
                            Endereco = omieCliente.Endereco,
                            EnderecoNumero = omieCliente.EnderecoNumero,
                            EnderecoComplemento = omieCliente.Complemento,
                            Bairro = omieCliente.Bairro,
                            Cep = omieCliente.Cep,
                            Estado = omieCliente.Estado,
                            Cidade = omieCliente.Cidade,
                            InscricaoEstadual = omieCliente.InscricaoEstadual,
                            InscricaoMunicipal = omieCliente.InscricaoMunicipal,
                            OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            OmieUpdatedAt = omieLastAlt
                        };
                        dbContext.Clientes.Add(novo);
                    }
                    else
                    {
                        if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                            existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                        {
                            return;
                        }

                        existing.RazaoSocial = omieCliente.RazaoSocial;
                        existing.NomeFantasia = omieCliente.NomeFantasia;
                        existing.CnpjCpf = omieCliente.CnpjCpf;
                        existing.Email = omieCliente.Email;
                        existing.Telefone = omieCliente.Telefone;
                        existing.Endereco = omieCliente.Endereco;
                        existing.EnderecoNumero = omieCliente.EnderecoNumero;
                        existing.EnderecoComplemento = omieCliente.Complemento;
                        existing.Bairro = omieCliente.Bairro;
                        existing.Cep = omieCliente.Cep;
                        existing.Estado = omieCliente.Estado;
                        existing.Cidade = omieCliente.Cidade;
                        existing.InscricaoEstadual = omieCliente.InscricaoEstadual;
                        existing.InscricaoMunicipal = omieCliente.InscricaoMunicipal;
                        existing.OptanteSimplesNacional = omieCliente.OptanteSimplesNacional == "S";
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

                    logger.LogInformation("Cliente/Fornecedor OmieId {OmieId} sincronizado individualmente com sucesso.", omieId);
                }
                else
                {
                    logger.LogWarning("Cliente/Fornecedor OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
                }
            }
            finally 
            {
                await lockService.ReleaseLockAsync(GetLockKey(omieId), lockToken, ct);
            }
        }

        private async Task<string?> AcquireResourceLockAsync(long omieId, CancellationToken ct, bool wait = true)
        {
            var lockKey = GetLockKey(omieId);
            var lockToken = Guid.NewGuid().ToString();

            if (await lockService.TryAcquireLockAsync(lockKey, lockToken, TimeSpan.FromMinutes(2), ct))
                return lockToken;

            if (!wait) return null;

            // SyncByIdAsync: aguarda até 30s (60 × 500ms) para garantir processamento de webhooks
            for (int i = 1; i < 60; i++) 
            {
                await Task.Delay(500, ct); 
                if (await lockService.TryAcquireLockAsync(lockKey, lockToken, TimeSpan.FromMinutes(2), ct))
                    return lockToken;
            }
            return null;
        }

        private string GetLockKey(long omieId) => $"sync:{EntityName}:{omieId}";

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
