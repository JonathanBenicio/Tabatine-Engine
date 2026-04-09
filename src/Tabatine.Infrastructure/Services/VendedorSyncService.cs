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
    public class VendedorSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<VendedorSyncService> _logger;

        public VendedorSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<VendedorSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Vendedores...");

            var lastSyncDate = await _syncState.GetLastSyncDateAsync("Vendedores", ct);
            var syncStartTime = DateTime.UtcNow;

            // Carrega lookup local para evitar N+1
            var existingLookup = await _dbContext.Vendedores.ToDictionaryAsync(v => v.OmieId, ct);
            int count = 0;

            await foreach (var omieItem in _omieClient.StreamVendedoresAsync(filtrarDe: lastSyncDate, cancellationToken: ct))
            {
                var omieId = omieItem.Codigo;
                var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieItem.DAlt, omieItem.HAlt);

                if (existingLookup.TryGetValue(omieId, out var existing))
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
                else
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
                    _dbContext.Vendedores.Add(novoVendedor);
                    existingLookup[omieId] = novoVendedor;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} vendedores processados...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("Vendedores", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Vendedores finalizada. Total: {Count}", count);
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Vendedor específico OmieId: {OmieId}", omieId);
            var omieVendedor = await _omieClient.ConsultarVendedorAsync(omieId, ct);

            if (omieVendedor != null)
            {
                var existing = await _dbContext.Vendedores.FirstOrDefaultAsync(v => v.OmieId == omieId, ct);
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
                    _dbContext.Vendedores.Add(novoVendedor);
                }
                else
                {
                    if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                        existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                    {
                        _logger.LogDebug("Vendedor OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieId);
                        return;
                    }

                    existing.Nome = omieVendedor.Nome;
                    existing.Email = omieVendedor.Email;
                    existing.Comissao = omieVendedor.Comissao;
                    existing.Inativo = omieVendedor.Inativo == "S";
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.OmieUpdatedAt = omieLastAlt;
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Vendedor OmieId {OmieId} sincronizado individualmente com sucesso.", omieId);
            }
            else
            {
                _logger.LogWarning("Vendedor OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
            }
        }
    }
}
