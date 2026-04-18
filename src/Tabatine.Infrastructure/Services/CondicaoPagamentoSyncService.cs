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
    public class CondicaoPagamentoSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState,
        ILogger<CondicaoPagamentoSyncService> logger) : ISyncService
    {
        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Condições de Pagamento...");

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarParcelasAsync(pagina, ct);

                if (response == null) break;

                var rawCadastros = response.Cadastros ?? new List<ParcelaOmie>();
                var processedOmieIds = new HashSet<long>();

                var codigos = rawCadastros.Where(c => c.Codigo > 0).Select(c => c.Codigo.ToString()).ToList();
                var existingCondicoes = await dbContext.CondicoesPagamento
                    .Where(c => codigos.Contains(c.Codigo))
                    .ToDictionaryAsync(c => c.Codigo, ct);

                foreach (var omieCondicao in rawCadastros)
                {
                    if (omieCondicao.Codigo <= 0) continue;
                    if (!processedOmieIds.Add(omieCondicao.Codigo)) continue;

                    var codigoStr = omieCondicao.Codigo.ToString();
                    existingCondicoes.TryGetValue(codigoStr, out var existing);

                    if (existing == null)
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
                            OmieId = omieCondicao.Codigo // Use the nCodigo as OmieId too
                        };
                        dbContext.CondicoesPagamento.Add(nova);
                        existingCondicoes[codigoStr] = nova;
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

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Página {Pagina} de {Total} de condições de pagamento sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("CondicaoPagamento", DateTime.UtcNow, ct);
            }
            logger.LogInformation("Sincronização de Condições de Pagamento finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogWarning("SyncById solicitado para CondicaoPagamento OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
