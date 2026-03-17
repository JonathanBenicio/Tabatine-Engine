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

            // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
            var processedOmieIds = new HashSet<long>();

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarContasCorrentesAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);
                
                if (response == null || response.ContasCorrentes == null || response.ContasCorrentes.Count == 0) break;

                var omieIds = response.ContasCorrentes.Select(c => c.Codigo).ToList();
                var existingContas = await _dbContext.ContasCorrente
                    .Where(c => omieIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                foreach (var omieItem in response.ContasCorrentes)
                {
                    var omieId = omieItem.Codigo;

                    // Pula se já processamos este OmieId neste ciclo (evita erros de constraint unique se a API repetir dados)
                    if (!processedOmieIds.Add(omieId))
                    {
                        _logger.LogDebug("Conta Corrente OmieId {OmieId} duplicada na resposta. Pulando.", omieId);
                        continue;
                    }

                    existingContas.TryGetValue(omieId, out var existing);

                    if (existing == null)
                    {
                        var novaConta = new ContaCorrente
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieId,
                            Descricao = omieItem.Descricao,
                            CodigoIntegracao = omieItem.CodigoIntegracao,
                            Tipo = omieItem.Tipo,
                            Inativa = omieItem.Inativo == "S",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _dbContext.ContasCorrente.Add(novaConta);
                        existingContas[omieId] = novaConta; // Adiciona ao dicionário local para evitar duplicatas se o ID se repetir na mesma página
                    }
                    else
                    {
                        existing.Descricao = omieItem.Descricao;
                        existing.CodigoIntegracao = omieItem.CodigoIntegracao;
                        existing.Tipo = omieItem.Tipo;
                        existing.Inativa = omieItem.Inativo == "S";
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de contas correntes sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("ContasCorrente", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Contas Correntes finalizada.");
        }
    }
}
