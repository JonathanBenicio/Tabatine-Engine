using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models.Geral;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class CaracteristicaSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<CaracteristicaSyncService> _logger;
        private readonly ISyncStateRepository _syncState;

        public CaracteristicaSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<CaracteristicaSyncService> logger, ISyncStateRepository syncState)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
            _syncState = syncState;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Características...");

            int pagina = 1;
            bool temMais = true;

            while (temMais)
            {
                var response = await _omieClient.ListarCaracteristicasAsync(pagina, ct);

                if (response?.ListaCaracteristicas == null || !response.ListaCaracteristicas.Any())
                {
                    break;
                }

                var omieIds = response.ListaCaracteristicas.Select(c => c.CodigoCaracteristica).ToList();
                var existingCaracteristicas = await _dbContext.Caracteristicas
                    .Include(c => c.Valores)
                    .Where(c => omieIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                foreach (var omieCaract in response.ListaCaracteristicas)
                {
                    if (existingCaracteristicas.TryGetValue(omieCaract.CodigoCaracteristica, out var existing))
                    {
                        existing.Nome = omieCaract.NomeCaracteristica;
                        existing.UpdatedAt = DateTime.UtcNow;

                        // Sincronizar Valores (Conteudos Permitidos)
                        SyncValores(existing, omieCaract.ConteudosPermitidos);
                    }
                    else
                    {
                        var nova = new Caracteristica
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieCaract.CodigoCaracteristica,
                            Nome = omieCaract.NomeCaracteristica,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        if (omieCaract.ConteudosPermitidos != null)
                        {
                            foreach (var valor in omieCaract.ConteudosPermitidos)
                            {
                                nova.Valores.Add(new CaracteristicaValor
                                {
                                    Id = Guid.NewGuid(),
                                    CaracteristicaId = nova.Id,
                                    Valor = valor.Conteudo,
                                    OmieIdConteudo = valor.IdConteudo
                                });
                            }
                        }

                        _dbContext.Caracteristicas.Add(nova);
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de Características sincronizada.", pagina);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("Caracteristicas", DateTime.UtcNow, ct);
            _logger.LogInformation("Sincronização de Características concluída.");
        }

        private void SyncValores(Caracteristica existing, List<OmieCaracteristicaConteudo>? omieValores)
        {
            if (omieValores == null) return;

            // Para simplificar: Remove os que não existem mais e adiciona ou atualiza os presentes
            var omieValoresIds = omieValores.Select(v => v.IdConteudo).ToList();
            
            // Remove antigos
            var toRemove = existing.Valores.Where(v => v.OmieIdConteudo.HasValue && !omieValoresIds.Contains(v.OmieIdConteudo.Value)).ToList();
            foreach (var r in toRemove) _dbContext.CaracteristicaValores.Remove(r);

            // Adiciona/Atualiza
            foreach (var omieValor in omieValores)
            {
                var existingValor = existing.Valores.FirstOrDefault(v => v.OmieIdConteudo == omieValor.IdConteudo);
                if (existingValor != null)
                {
                    existingValor.Valor = omieValor.Conteudo;
                }
                else
                {
                    existing.Valores.Add(new CaracteristicaValor
                    {
                        Id = Guid.NewGuid(),
                        CaracteristicaId = existing.Id,
                        Valor = omieValor.Conteudo,
                        OmieIdConteudo = omieValor.IdConteudo
                    });
                }
            }
        }
    }
}
