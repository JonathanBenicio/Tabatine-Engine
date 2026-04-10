using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class MeioPagamentoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<MeioPagamentoSyncService> _logger;

        public MeioPagamentoSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<MeioPagamentoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Meios de Pagamento...");

            var response = await _omieClient.ListarMeiosPagamentoAsync(ct);

            if (response?.MeiosPagamentoLista == null || !response.MeiosPagamentoLista.Any())
            {
                _logger.LogInformation("Nenhum meio de pagamento encontrado na Omie.");
                return;
            }

            var processedCodes = new HashSet<string>();
            var existingMeios = await _dbContext.MeiosPagamento.ToDictionaryAsync(m => m.Codigo, ct);

            foreach (var omieMeio in response.MeiosPagamentoLista)
            {
                if (string.IsNullOrWhiteSpace(omieMeio.Codigo)) continue;

                if (!processedCodes.Add(omieMeio.Codigo))
                {
                    _logger.LogWarning("Meio de Pagamento Código {Cod} duplicado na resposta da Omie. Pulando.", omieMeio.Codigo);
                    continue;
                }

                if (existingMeios.TryGetValue(omieMeio.Codigo, out var existingMeio))
                {
                    existingMeio.Descricao = omieMeio.Descricao;
                    existingMeio.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Stable ID generation using deterministic character sum
                    long omieId = 0;
                    foreach (char c in omieMeio.Codigo) omieId = (omieId * 31) + c;
                    omieId = Math.Abs(omieId);
                    var novo = new MeioPagamento
                    {
                        Id = Guid.NewGuid(),
                        OmieId = omieId,
                        Codigo = omieMeio.Codigo,
                        Descricao = omieMeio.Descricao,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.MeiosPagamento.Add(novo);
                    existingMeios[omieMeio.Codigo] = novo;
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Sincronização de Meios de Pagamento finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogWarning("SyncById solicitado para MeioPagamento OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
