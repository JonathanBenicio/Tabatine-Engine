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
    public class ContaCorrenteSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<ContaCorrenteSyncService> _logger;

        public ContaCorrenteSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<ContaCorrenteSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Contas Correntes...");
            
            var lastSyncDate = await _syncState.GetLastSyncDateAsync("ContasCorrente", ct);
            var syncStartTime = DateTime.UtcNow;

            // Carrega lookups locais para evitar N+1
            var existingLookup = await _dbContext.ContasCorrente.ToDictionaryAsync(c => c.OmieId, ct);
            var bancosLookup = await _dbContext.Bancos.ToDictionaryAsync(b => b.CodigoBanco, ct);
            int count = 0;

            await foreach (var omieItem in _omieClient.StreamContasCorrentesAsync(filtrarDe: lastSyncDate, cancellationToken: ct))
            {
                var omieId = omieItem.Codigo;
                
                bancosLookup.TryGetValue(omieItem.CodigoBanco ?? string.Empty, out var banco);

                if (existingLookup.TryGetValue(omieId, out var existing))
                {
                    existing.Descricao = omieItem.Descricao;
                    existing.CodigoIntegracao = omieItem.CodigoIntegracao;
                    existing.Tipo = omieItem.Tipo;
                    existing.Inativa = omieItem.Inativo == "S";
                    existing.BancoId = banco?.Id;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var novaConta = new ContaCorrente
                    {
                        Id = Guid.NewGuid(),
                        OmieId = omieId,
                        Descricao = omieItem.Descricao,
                        CodigoIntegracao = omieItem.CodigoIntegracao,
                        Tipo = omieItem.Tipo,
                        Inativa = omieItem.Inativo == "S",
                        BancoId = banco?.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.ContasCorrente.Add(novaConta);
                    existingLookup[omieId] = novaConta;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} contas correntes processadas...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("ContasCorrente", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Contas Correntes finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogWarning("SyncById solicitado para ContaCorrente OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }
    }
}
