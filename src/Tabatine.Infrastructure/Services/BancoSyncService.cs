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

            var processedCodes = new HashSet<string>();
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarBancosAsync(pagina, ct);
                
                if (response == null || response.Bancos == null || response.Bancos.Count == 0) break;

                var codigos = response.Bancos.Select(b => b.Codigo).ToList();
                var existingBancos = await _dbContext.Bancos
                    .Where(b => codigos.Contains(b.CodigoBanco))
                    .ToDictionaryAsync(b => b.CodigoBanco, ct);

                foreach (var omieBanco in response.Bancos)
                {
                    if (!processedCodes.Add(omieBanco.Codigo))
                    {
                        _logger.LogWarning("Banco Código {Cod} duplicado na resposta da Omie. Pulando.", omieBanco.Codigo);
                        continue;
                    }

                    existingBancos.TryGetValue(omieBanco.Codigo, out var existing);

                    if (existing == null)
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
                        existingBancos[omieBanco.Codigo] = novoBanco;
                    }
                    else
                    {
                        existing.Nome = omieBanco.Nome;
                        existing.CodigoIspb = omieBanco.CodigoIspb;
                        existing.Tipo = omieBanco.Tipo;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de bancos sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("Bancos", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Bancos finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Banco específico OmieId: {OmieId}", omieId);
            
            // Omie não tem consulta individual para Bancos, listamos tudo
            var response = await _omieClient.ListarBancosAsync(1, ct);
            if (response?.Bancos == null) return;

            var omieBanco = response.Bancos.FirstOrDefault(b => long.TryParse(b.Codigo, out var id) && id == omieId);
            if (omieBanco == null)
            {
                _logger.LogWarning("Banco OmieId {OmieId} não encontrado na listagem da Omie.", omieId);
                return;
            }

            var existing = await _dbContext.Bancos.FirstOrDefaultAsync(b => b.CodigoBanco == omieBanco.Codigo, ct);

            if (existing == null)
            {
                var novoBanco = new Banco
                {
                    Id = Guid.NewGuid(),
                    OmieId = omieId,
                    CodigoBanco = omieBanco.Codigo,
                    Nome = omieBanco.Nome,
                    CodigoIspb = omieBanco.CodigoIspb,
                    Tipo = omieBanco.Tipo,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Bancos.Add(novoBanco);
            }
            else
            {
                existing.Nome = omieBanco.Nome;
                existing.CodigoIspb = omieBanco.CodigoIspb;
                existing.Tipo = omieBanco.Tipo;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Banco OmieId {OmieId} sincronizado com sucesso.", omieId);
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
