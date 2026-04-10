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
        private readonly ILogger<VendedorSyncService> _logger;

        public VendedorSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<VendedorSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Vendedores...");

            // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
            var processedOmieIds = new HashSet<long>();

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarVendedoresAsync(pagina, cancellationToken: ct);

                if (response == null || response.Vendedores == null || response.Vendedores.Count == 0) break;

                var omieIds = response.Vendedores.Select(v => v.Codigo).ToList();
                var existingVendedores = await _dbContext.Vendedores
                    .Where(v => omieIds.Contains(v.OmieId))
                    .ToDictionaryAsync(v => v.OmieId, ct);

                foreach (var omieItem in response.Vendedores)
                {
                    var omieId = omieItem.Codigo;

                    // Pula se já processamos este OmieId neste ciclo
                    if (!processedOmieIds.Add(omieId))
                    {
                        _logger.LogDebug("Vendedor OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
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
                        _dbContext.Vendedores.Add(novoVendedor);
                        existingVendedores[omieId] = novoVendedor;
                    }
                    else
                    {
                        // Se o timestamp da Omie for igual ao que já temos, pula o update
                        if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                            existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                        {
                            _logger.LogDebug("Vendedor OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieId);
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

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de vendedores sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            _logger.LogInformation("Sincronização de Vendedores finalizada.");
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

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
