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
using Tabatine.Omie.Client.Models.Geral;

namespace Tabatine.Infrastructure.Services
{
    public class CondicaoPagamentoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<CondicaoPagamentoSyncService> _logger;

        public CondicaoPagamentoSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<CondicaoPagamentoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Condições de Pagamento...");

            var syncStartTime = DateTime.UtcNow;

            // Carrega lookup local para evitar N+1
            var existingLookup = await _dbContext.CondicoesPagamento.ToDictionaryAsync(c => c.Codigo, ct);
            int count = 0;

            await foreach (var omieCondicao in _omieClient.StreamParcelasAsync(ct))
            {
                if (omieCondicao.Codigo <= 0) continue;

                var codigoStr = omieCondicao.Codigo.ToString();
                if (existingLookup.TryGetValue(codigoStr, out var existing))
                {
                    existing.Descricao = omieCondicao.Descricao;
                    existing.QuantidadeParcelas = omieCondicao.QuantidadeParcelas;
                    existing.DiaFixo = omieCondicao.DiaFixo;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.OmieId = omieCondicao.Codigo;
                }
                else
                {
                    var nova = new CondicaoPagamento
                    {
                        Id = Guid.NewGuid(),
                        Codigo = codigoStr,
                        Descricao = omieCondicao.Descricao,
                        QuantidadeParcelas = omieCondicao.QuantidadeParcelas,
                        DiaFixo = omieCondicao.DiaFixo,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OmieId = omieCondicao.Codigo
                    };
                    _dbContext.CondicoesPagamento.Add(nova);
                    existingLookup[codigoStr] = nova;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} condições de pagamento processadas...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("CondicoesPagamento", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Condições de Pagamento finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogWarning("SyncById solicitado para CondicaoPagamento OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }
    }
}
