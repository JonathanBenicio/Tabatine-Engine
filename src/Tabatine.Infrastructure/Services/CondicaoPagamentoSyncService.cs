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
                
                // Omie returns nCodigo as int. We treat it as string in our DB.
                var validCadastros = rawCadastros
                    .Where(c => c.Codigo > 0)
                    .GroupBy(c => c.Codigo)
                    .Select(g => g.First())
                    .ToList();

                var codigos = validCadastros.Select(c => c.Codigo.ToString()).ToList();
                var existingCondicoes = await _dbContext.CondicoesPagamento
                    .Where(c => codigos.Contains(c.Codigo))
                    .ToDictionaryAsync(c => c.Codigo, ct);

                foreach (var omieCondicao in validCadastros)
                {
                    var codigoStr = omieCondicao.Codigo.ToString();
                    existingCondicoes.TryGetValue(codigoStr, out var existing);

                    if (existing == null)
                    {
                        _dbContext.CondicoesPagamento.Add(new CondicaoPagamento
                        {
                            Id = Guid.NewGuid(),
                            Codigo = codigoStr,
                            Descricao = omieCondicao.Descricao,
                            QuantidadeParcelas = omieCondicao.QuantidadeParcelas,
                            DiaFixo = omieCondicao.DiaFixo,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            OmieId = omieCondicao.Codigo // Use the nCodigo as OmieId too
                        });
                    }
                    else
                    {
                        existing.Descricao = omieCondicao.Descricao;
                        existing.QuantidadeParcelas = omieCondicao.QuantidadeParcelas;
                        existing.DiaFixo = omieCondicao.DiaFixo;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.OmieId = omieCondicao.Codigo;
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
