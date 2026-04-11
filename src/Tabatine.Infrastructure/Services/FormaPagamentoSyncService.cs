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
    public class FormaPagamentoSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState,
        ILogger<FormaPagamentoSyncService> logger) : ISyncService
    {
        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Formas de Pagamento...");

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarFormasPagVendasAsync(pagina, ct);
                
                if (response == null) break;

                var rawFormas = response.FormasPagamento ?? new List<OmieFormaPagamento>();
                var processedCodes = new HashSet<string>();

                var codigos = rawFormas.Where(f => !string.IsNullOrWhiteSpace(f.Codigo)).Select(f => f.Codigo).ToList();
                var existingFormas = await dbContext.FormasPagamento
                    .Where(f => codigos.Contains(f.Codigo))
                    .ToDictionaryAsync(f => f.Codigo, ct);

                foreach (var omieForma in rawFormas)
                {
                    if (string.IsNullOrWhiteSpace(omieForma.Codigo)) continue;
                    if (!processedCodes.Add(omieForma.Codigo))
                    {
                        logger.LogWarning("Forma de Pagamento Código {Cod} duplicado na resposta da Omie. Pulando.", omieForma.Codigo);
                        continue;
                    }

                    existingFormas.TryGetValue(omieForma.Codigo, out var existing);

                    if (existing == null)
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
                        dbContext.FormasPagamento.Add(nova);
                        existingFormas[omieForma.Codigo] = nova;
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

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Página {Pagina} de {Total} de formas de pagamento sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await syncState.SetLastSyncDateAsync("FormaPagamento", DateTime.UtcNow, ct);
            logger.LogInformation("Sincronização de Formas de Pagamento finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogWarning("SyncById solicitado para FormaPagamento OmieId={OmieId}. A Omie não possui endpoint de consulta individual para esta entidade. Requisição ignorada.", omieId);
            await Task.CompletedTask;
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
