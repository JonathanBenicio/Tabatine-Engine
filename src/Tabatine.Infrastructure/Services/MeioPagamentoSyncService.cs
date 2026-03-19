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
        private readonly ISyncStateRepository _syncState;

        public MeioPagamentoSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<MeioPagamentoSyncService> logger, ISyncStateRepository syncState)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
            _syncState = syncState;
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

            var existingMeios = await _dbContext.MeiosPagamento.ToDictionaryAsync(m => m.Codigo, ct);

            foreach (var omieMeio in response.MeiosPagamentoLista)
            {
                if (existingMeios.TryGetValue(omieMeio.Codigo, out var existingMeio))
                {
                    existingMeio.Descricao = omieMeio.Descricao;
                    existingMeio.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    _dbContext.MeiosPagamento.Add(new MeioPagamento
                    {
                        Id = Guid.NewGuid(),
                        OmieId = 0, // Meios de pagamento não possuem um long ID único na API de listagem, usamos o código como chave
                        Codigo = omieMeio.Codigo,
                        Descricao = omieMeio.Descricao,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            await _syncState.SetLastSyncDateAsync("MeiosPagamento", DateTime.UtcNow, ct);
            _logger.LogInformation("Sincronização de Meios de Pagamento concluída.");
        }
    }
}
