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

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarParcelasAsync(pagina, ct);

                if (response == null) break;

                var rawCadastros = response.Cadastros ?? new List<ParcelaOmie>();
                
                var invalidCount = rawCadastros.Count(c => string.IsNullOrWhiteSpace(c.Codigo));
                if (invalidCount > 0)
                {
                    _logger.LogWarning("{Count} condições de pagamento ignoradas por falta de código.", invalidCount);
                }

                var validCadastros = rawCadastros
                    .Where(c => !string.IsNullOrWhiteSpace(c.Codigo))
                    .GroupBy(c => c.Codigo)
                    .Select(g => g.First())
                    .ToList();

                if (validCadastros.Count < (rawCadastros.Count() - invalidCount))
                {
                    _logger.LogWarning("{Count} condições de pagamento com código duplicado ignoradas na página {Pagina}.", 
                        rawCadastros.Count() - invalidCount - validCadastros.Count, pagina);
                }

                var codigos = validCadastros.Select(c => c.Codigo).ToList();
                var existingCondicoes = await _dbContext.CondicoesPagamento
                    .Where(c => codigos.Contains(c.Codigo))
                    .ToDictionaryAsync(c => c.Codigo, ct);

                foreach (var omieCondicao in validCadastros)
                {
                    existingCondicoes.TryGetValue(omieCondicao.Codigo, out var existing);

                    if (existing == null)
                    {
                        _dbContext.CondicoesPagamento.Add(new CondicaoPagamento
                        {
                            Id = Guid.NewGuid(),
                            Codigo = omieCondicao.Codigo,
                            Descricao = omieCondicao.Descricao,
                            QuantidadeParcelas = omieCondicao.QuantidadeParcelas,
                            DiaFixo = omieCondicao.DiaFixo,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            OmieId = 0 // ParcelaAPI doesn't return integer ID, assume 0 as placeholder for standard
                        });
                    }
                    else
                    {
                        existing.Descricao = omieCondicao.Descricao;
                        existing.QuantidadeParcelas = omieCondicao.QuantidadeParcelas;
                        existing.DiaFixo = omieCondicao.DiaFixo;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de condições de pagamento sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("CondicoesPagamento", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Condições de Pagamento finalizada.");
        }
    }
}
