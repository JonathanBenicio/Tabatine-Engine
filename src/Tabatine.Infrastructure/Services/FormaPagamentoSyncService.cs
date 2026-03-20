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

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarFormasPagVendasAsync(pagina, ct);
                
                if (response == null) break;

                var rawFormas = response.FormasPagamento ?? new List<OmieFormaPagamento>();

                var invalidCount = rawFormas.Count(f => string.IsNullOrWhiteSpace(f.Codigo));
                if (invalidCount > 0)
                {
                    _logger.LogWarning("{Count} formas de pagamento ignoradas por falta de código.", invalidCount);
                }

                var validFormas = rawFormas
                    .Where(f => !string.IsNullOrWhiteSpace(f.Codigo))
                    .GroupBy(f => f.Codigo)
                    .Select(g => g.First())
                    .ToList();

                if (validFormas.Count < (rawFormas.Count - invalidCount))
                {
                    _logger.LogWarning("{Count} formas de pagamento com código duplicado ignoradas na página {Pagina}.", 
                        rawFormas.Count - invalidCount - validFormas.Count, pagina);
                }

                var codigos = validFormas.Select(f => f.Codigo).ToList();
                var existingFormas = await _dbContext.FormasPagamento
                    .Where(f => codigos.Contains(f.Codigo))
                    .ToDictionaryAsync(f => f.Codigo, ct);

                foreach (var omieForma in validFormas)
                {
                    existingFormas.TryGetValue(omieForma.Codigo, out var existing);

                    if (existing == null)
                    {
                        _dbContext.FormasPagamento.Add(new FormaPagamento
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
                        });
                    }
                    else
                    {
                        existing.Descricao = omieForma.Descricao;
                        existing.QuantidadeParcelas = omieForma.QuantidadeParcelas;
                        existing.DiasParcelas = omieForma.DiasParcelas;
                        existing.ListaParcelas = omieForma.ListaParcelas;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de formas de pagamento sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("FormasPagamento", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Formas de Pagamento finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default) => await Task.CompletedTask;
    }
}
