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
            
            var syncStartTime = DateTime.UtcNow;

            // Carrega lookup local para evitar N+1 (Chave composta: CodigoOperacao + CodigoEtapa)
            var existingLookup = await _dbContext.EtapasFaturamento
                .ToDictionaryAsync(e => $"{e.CodigoOperacao}-{e.Codigo}", ct);
            
            int count = 0;

            await foreach (var operacao in _omieClient.StreamEtapasFaturamentoAsync(ct))
            {
                var codOperacao = operacao.CodigoOperacao;
                var descOperacao = operacao.DescricaoOperacao;

                foreach (var omieEtapa in operacao.Etapas)
                {
                    var key = $"{codOperacao}-{omieEtapa.Codigo}";
                    
                    if (existingLookup.TryGetValue(key, out var existing))
                    {
                        existing.Descricao = omieEtapa.Descricao;
                        existing.DescricaoPadrao = omieEtapa.DescricaoPadrao;
                        existing.Inativa = omieEtapa.Inativo == "S";
                        existing.DescricaoOperacao = descOperacao;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
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
                            OmieId = 0
                        };
                        _dbContext.EtapasFaturamento.Add(nova);
                        existingLookup[key] = nova;
                    }

                    count++;
                }

                if (count > 0 && count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} etapas processadas...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("EtapasFaturamento", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Etapas de Faturamento finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogWarning("SyncById solicitado para EtapaFaturamento OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }
    }
}
