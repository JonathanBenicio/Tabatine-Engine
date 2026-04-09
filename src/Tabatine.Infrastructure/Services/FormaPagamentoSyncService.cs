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
using Tabatine.Omie.Client.Models.FormaPagamento;

namespace Tabatine.Infrastructure.Services
{
    public class FormaPagamentoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<FormaPagamentoSyncService> _logger;

        public FormaPagamentoSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<FormaPagamentoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Formas de Pagamento...");
            
            var syncStartTime = DateTime.UtcNow;

            // Carrega lookup local para evitar N+1
            var existingLookup = await _dbContext.FormasPagamento.ToDictionaryAsync(f => f.Codigo, ct);
            int count = 0;

            await foreach (var omieForma in _omieClient.StreamFormasPagVendasAsync(ct))
            {
                if (string.IsNullOrWhiteSpace(omieForma.Codigo)) continue;

                if (existingLookup.TryGetValue(omieForma.Codigo, out var existing))
                {
                    existing.Descricao = omieForma.Descricao;
                    existing.QuantidadeParcelas = omieForma.QuantidadeParcelas;
                    existing.DiasParcelas = omieForma.DiasParcelas;
                    existing.ListaParcelas = omieForma.ListaParcelas;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var nova = new FormaPagamento
                    {
                        Id = Guid.NewGuid(),
                        Codigo = omieForma.Codigo,
                        Descricao = omieForma.Descricao,
                        QuantidadeParcelas = omieForma.QuantidadeParcelas,
                        DiasParcelas = omieForma.DiasParcelas,
                        ListaParcelas = omieForma.ListaParcelas,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OmieId = 0
                    };
                    _dbContext.FormasPagamento.Add(nova);
                    existingLookup[omieForma.Codigo] = nova;
                }

                if (++count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} formas de pagamento processadas...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("FormasPagamento", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Formas de Pagamento finalizada. Total: {Count}", count);
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogWarning("SyncById solicitado para FormaPagamento OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }
    }
}
