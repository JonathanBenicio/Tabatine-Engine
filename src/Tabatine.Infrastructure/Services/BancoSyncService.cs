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
    public class BancoSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState,
        ILogger<BancoSyncService> logger) : ISyncService
    {

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Bancos...");

            var processedCodes = new HashSet<string>();
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarBancosAsync(pagina, ct);
                
                if (response == null || response.Bancos == null || response.Bancos.Count == 0) break;

                var codigos = response.Bancos.Select(b => b.Codigo).ToList();
                var existingBancos = await dbContext.Bancos
                    .Where(b => codigos.Contains(b.CodigoBanco))
                    .ToDictionaryAsync(b => b.CodigoBanco, ct);

                foreach (var omieBanco in response.Bancos)
                {
                    if (!processedCodes.Add(omieBanco.Codigo))
                    {
                        logger.LogWarning("Banco Código {Cod} duplicado na resposta da Omie. Pulando.", omieBanco.Codigo);
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

                        dbContext.Bancos.Add(novoBanco);
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

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Página {Pagina} de {Total} de bancos sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("Bancos", DateTime.UtcNow, ct);
            }
            logger.LogInformation("Sincronização de Bancos finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogInformation("Sincronizando Banco específico OmieId: {OmieId}", omieId);
            
            // Omie não tem consulta individual para Bancos, listamos tudo
            var response = await omieClient.ListarBancosAsync(1, ct);
            if (response?.Bancos == null) return;

            var omieBanco = response.Bancos.FirstOrDefault(b => long.TryParse(b.Codigo, out var id) && id == omieId);
            if (omieBanco == null)
            {
                logger.LogWarning("Banco OmieId {OmieId} não encontrado na listagem da Omie.", omieId);
                return;
            }

            var existing = await dbContext.Bancos.FirstOrDefaultAsync(b => b.CodigoBanco == omieBanco.Codigo, ct);

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
                dbContext.Bancos.Add(novoBanco);
            }
            else
            {
                existing.Nome = omieBanco.Nome;
                existing.CodigoIspb = omieBanco.CodigoIspb;
                existing.Tipo = omieBanco.Tipo;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Banco OmieId {OmieId} sincronizado com sucesso.", omieId);
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
