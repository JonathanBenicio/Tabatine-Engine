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
    public class EtapaFaturamentoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<EtapaFaturamentoSyncService> _logger;

        public EtapaFaturamentoSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<EtapaFaturamentoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Etapas de Faturamento...");
            
            // Etapas geralmente não filtram por data, sincronizamos tudo
            var syncStartTime = DateTime.UtcNow;

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarEtapasFaturamentoAsync(pagina, ct);
                
                if (response == null || response.Cadastros == null || response.Cadastros.Count == 0) break;

                // Batch load ALL existing etapas for ALL operations in this page
                var allCodOperacao = response.Cadastros.Select(c => c.CodigoOperacao).ToList();
                var allCodEtapas = response.Cadastros.SelectMany(c => c.Etapas.Select(e => e.Codigo)).Distinct().ToList();

                var existingEtapas = await _dbContext.EtapasFaturamento
                    .Where(e => allCodOperacao.Contains(e.CodigoOperacao) && allCodEtapas.Contains(e.Codigo))
                    .ToListAsync(ct);

                // Dictionary keyed by [CodigoOperacao-CodigoEtapa] for unique matching
                var existingDict = existingEtapas.ToDictionary(e => $"{e.CodigoOperacao}-{e.Codigo}");
                var processedKeys = new HashSet<string>();

                foreach (var operacao in response.Cadastros)
                {
                    var codOperacao = operacao.CodigoOperacao;
                    var descOperacao = operacao.DescricaoOperacao;

                    foreach (var omieEtapa in operacao.Etapas)
                    {
                        var key = $"{codOperacao}-{omieEtapa.Codigo}";
                        if (!processedKeys.Add(key)) continue;

                        existingDict.TryGetValue(key, out var existing);

                        if (existing == null)
                        {
                            var nova = new EtapaFaturamento
                            {
                                Id = Guid.NewGuid(),
                                Codigo = omieEtapa.Codigo,
                                Descricao = omieEtapa.Descricao,
                                DescricaoPadrao = omieEtapa.DescricaoPadrao,
                                Inativa = omieEtapa.Inativo == "S",
                                CodigoOperacao = codOperacao,
                                DescricaoOperacao = descOperacao,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                OmieId = 0 // Não há ID numérico para etapas no endpoint Listar
                            };
                            _dbContext.EtapasFaturamento.Add(nova);
                            existingDict[key] = nova;
                        }
                        else
                        {
                            existing.Descricao = omieEtapa.Descricao;
                            existing.DescricaoPadrao = omieEtapa.DescricaoPadrao;
                            existing.Inativa = omieEtapa.Inativo == "S";
                            existing.DescricaoOperacao = descOperacao;
                            existing.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de etapas sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("EtapasFaturamento", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Etapas de Faturamento finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default) => await Task.CompletedTask;
    }
}
