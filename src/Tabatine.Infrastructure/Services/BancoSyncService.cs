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
    public class BancoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<BancoSyncService> _logger;

        public BancoSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<BancoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Bancos...");
            
            var syncStartTime = DateTime.UtcNow;
            
            // Carrega lookup local para evitar N+1
            var existingLookup = await _dbContext.Bancos.ToDictionaryAsync(b => b.CodigoBanco, ct);
            int count = 0;

            await foreach (var omieBanco in _omieClient.StreamBancosAsync(ct))
            {
                if (existingLookup.TryGetValue(omieBanco.Codigo, out var existing))
                {
                    existing.Nome = omieBanco.Nome;
                    existing.CodigoIspb = omieBanco.CodigoIspb;
                    existing.Tipo = omieBanco.Tipo;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var novoBanco = new Banco
                    {
                        Id = Guid.NewGuid(),
                        CodigoBanco = omieBanco.Codigo,
                        Nome = omieBanco.Nome,
                        CodigoIspb = omieBanco.CodigoIspb,
                        Tipo = omieBanco.Tipo,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (long.TryParse(omieBanco.Codigo, out long omieId))
                    {
                        novoBanco.OmieId = omieId;
                    }

                    _dbContext.Bancos.Add(novoBanco);
                    existingLookup[omieBanco.Codigo] = novoBanco;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} bancos processados...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("Bancos", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Bancos finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogWarning("SyncById solicitado para Banco OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }
    }
}
