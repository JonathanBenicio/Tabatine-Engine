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
    public class ContaCorrenteSyncService(
        IOmieClient omieClient, 
        AppDbContext dbContext, 
        ISyncStateRepository syncState,
        ILogger<ContaCorrenteSyncService> logger) : ISyncService
    {
        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            logger.LogInformation("Iniciando sincronização de Contas Correntes...");

            // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
            var processedOmieIds = new HashSet<long>();

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await omieClient.ListarContasCorrentesAsync(pagina, cancellationToken: ct);
                
                if (response == null || response.ContasCorrentes == null || response.ContasCorrentes.Count == 0) break;

                var omieIds = response.ContasCorrentes.Select(c => c.Codigo).ToList();
                var omieBancoCodigos = response.ContasCorrentes.Where(c => c.CodigoBanco != null).Select(c => c.CodigoBanco!).Distinct().ToList();

                var existingContas = await dbContext.ContasCorrente
                    .Where(c => omieIds.Contains(c.OmieId))
                    .ToDictionaryAsync(c => c.OmieId, ct);

                var bancos = await dbContext.Bancos
                    .Where(b => omieBancoCodigos.Contains(b.CodigoBanco))
                    .ToDictionaryAsync(b => b.CodigoBanco, ct);

                foreach (var omieItem in response.ContasCorrentes)
                {
                    var omieId = omieItem.Codigo;

                    // Pula se já processamos este OmieId neste ciclo (evita erros de constraint unique se a API repetir dados)
                    if (!processedOmieIds.Add(omieId))
                    {
                        logger.LogDebug("Conta Corrente OmieId {OmieId} duplicada na resposta. Pulando.", omieId);
                        continue;
                    }

                    existingContas.TryGetValue(omieId, out var existing);
                    bancos.TryGetValue(omieItem.CodigoBanco ?? string.Empty, out var banco);

                    if (existing == null)
                    {
                        var novaConta = new ContaCorrente
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieId,
                            Descricao = omieItem.Descricao,
                            CodigoIntegracao = omieItem.CodigoIntegracao,
                            Tipo = omieItem.Tipo,
                            Inativa = omieItem.Inativo == "S",
                            BancoId = banco?.Id,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        dbContext.ContasCorrente.Add(novaConta);
                        existingContas[omieId] = novaConta; // Adiciona ao dicionário local para evitar duplicatas se o ID se repetir na mesma página
                    }
                    else
                    {
                        existing.Descricao = omieItem.Descricao;
                        existing.CodigoIntegracao = omieItem.CodigoIntegracao;
                        existing.Tipo = omieItem.Tipo;
                        existing.Inativa = omieItem.Inativo == "S";
                        existing.BancoId = banco?.Id;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                logger.LogInformation("Página {Pagina} de {Total} de contas correntes sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            if (!ct.IsCancellationRequested)
            {
                await syncState.SetLastSyncDateAsync("ContasCorrente", DateTime.UtcNow, ct);
            }

            logger.LogInformation("Sincronização de Contas Correntes finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            logger.LogInformation("Sincronizando Conta Corrente específica OmieId: {OmieId}", omieId);
            
            // Omie não tem consulta individual para Conta Corrente, buscamos na listagem
            var response = await omieClient.ListarContasCorrentesAsync(1, cancellationToken: ct);
            if (response?.ContasCorrentes == null) return;

            var omieItem = response.ContasCorrentes.FirstOrDefault(c => c.Codigo == omieId);
            if (omieItem == null)
            {
                logger.LogWarning("Conta Corrente OmieId {OmieId} não encontrada na listagem da Omie.", omieId);
                return;
            }

            var existing = await dbContext.ContasCorrente.FirstOrDefaultAsync(c => c.OmieId == omieId, ct);
            
            var omieBancoCodigos = new List<string> { omieItem.CodigoBanco ?? string.Empty };
            var banco = await dbContext.Bancos.FirstOrDefaultAsync(b => b.CodigoBanco == omieItem.CodigoBanco, ct);

            if (existing == null)
            {
                var novaConta = new ContaCorrente
                {
                    Id = Guid.NewGuid(),
                    OmieId = omieId,
                    Descricao = omieItem.Descricao,
                    CodigoIntegracao = omieItem.CodigoIntegracao,
                    Tipo = omieItem.Tipo,
                    Inativa = omieItem.Inativo == "S",
                    SaldoInicial = omieItem.SaldoInicial,
                    BancoId = banco?.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                dbContext.ContasCorrente.Add(novaConta);
            }
            else
            {
                existing.Descricao = omieItem.Descricao;
                existing.CodigoIntegracao = omieItem.CodigoIntegracao;
                existing.Tipo = omieItem.Tipo;
                existing.Inativa = omieItem.Inativo == "S";
                existing.SaldoInicial = omieItem.SaldoInicial;
                existing.BancoId = banco?.Id;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync(ct);
            logger.LogInformation("Conta Corrente OmieId {OmieId} sincronizada com sucesso.", omieId);
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
