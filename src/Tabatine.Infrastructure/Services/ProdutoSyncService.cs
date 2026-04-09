using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class ProdutoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ISyncStateRepository _syncState;
        private readonly ILogger<ProdutoSyncService> _logger;

        public ProdutoSyncService(IOmieClient omieClient, AppDbContext dbContext, ISyncStateRepository syncState, ILogger<ProdutoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _syncState = syncState;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Produtos...");

            var lastSyncDate = await _syncState.GetLastSyncDateAsync("Produtos", ct);
            var syncStartTime = DateTime.UtcNow;

            // Rastreia OmieIds já processados neste ciclo para evitar duplicatas
            var processedOmieIds = new HashSet<long>();

            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarProdutosAsync(pagina, filtrarDe: lastSyncDate, cancellationToken: ct);

                // Resposta nula = sem registros (Client-5113)
                if (response == null || response.ProdutosCadastro == null || response.ProdutosCadastro.Count == 0) break;

                var omieIds = response.ProdutosCadastro.Select(p => p.CodigoProduto).ToList();
                var caracNames = response.ProdutosCadastro.SelectMany(p => p.Caracteristicas?.Select(c => c.NomeCaracteristica) ?? Enumerable.Empty<string>()).Distinct().ToList();
                var caracValues = response.ProdutosCadastro.SelectMany(p => p.Caracteristicas?.Select(c => c.ValorCaracteristica) ?? Enumerable.Empty<string>()).Distinct().ToList();

                var existingProdutos = await _dbContext.Produtos
                    .Where(p => omieIds.Contains(p.OmieId))
                    .ToDictionaryAsync(p => p.OmieId, ct);

                foreach (var omieItem in response.ProdutosCadastro)
                {
                    var omieId = omieItem.CodigoProduto;

                    // Pula se já processamos este OmieId neste ciclo
                    if (!processedOmieIds.Add(omieId))
                    {
                        _logger.LogDebug("Produto OmieId {OmieId} duplicado na resposta. Pulando.", omieId);
                        continue;
                    }

                    existingProdutos.TryGetValue(omieId, out var existing);
                    var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieItem.DAlt, omieItem.HAlt);

                    if (existing == null)
                    {
                        var novoProduto = new Produto
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieId,
                            CodigoProduto = omieItem.Codigo,
                            Descricao = omieItem.Descricao,
                            PrecoUnitario = omieItem.ValorUnitario,
                            Ncm = omieItem.Ncm,
                            UnidadeMedida = omieItem.Unidade,
                            PesoLiquido = omieItem.PesoLiquido,
                            PesoBruto = omieItem.PesoBruto,
                            FamiliaProduto = omieItem.FamiliaProduto,
                            Ativo = omieItem.Inativo == "N",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            OmieUpdatedAt = omieLastAlt
                        };
                        _dbContext.Produtos.Add(novoProduto);
                        existingProdutos[omieId] = novoProduto;
                    }
                    else
                    {
                        // Se o timestamp da Omie for igual ao que já temos, pula o update
                        if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                            existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                        {
                            _logger.LogDebug("Produto OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieId);
                            continue;
                        }

                        existing.CodigoProduto = omieItem.Codigo;
                        existing.Descricao = omieItem.Descricao;
                        existing.PrecoUnitario = omieItem.ValorUnitario;
                        existing.Ncm = omieItem.Ncm;
                        existing.UnidadeMedida = omieItem.Unidade;
                        existing.PesoLiquido = omieItem.PesoLiquido;
                        existing.PesoBruto = omieItem.PesoBruto;
                        existing.FamiliaProduto = omieItem.FamiliaProduto;
                        existing.Ativo = omieItem.Inativo == "N";
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.OmieUpdatedAt = omieLastAlt;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de {Total} de produtos sincronizada.", pagina, response.TotalDePaginas);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            await _syncState.SetLastSyncDateAsync("Produtos", syncStartTime, ct);
            _logger.LogInformation("Sincronização de Produtos finalizada.");
        }
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Produto específico OmieId: {OmieId}", omieId);
            var omieProduto = await _omieClient.ConsultarProdutoAsync(omieId, ct);

            if (omieProduto != null)
            {
                var existing = await _dbContext.Produtos.FirstOrDefaultAsync(p => p.OmieId == omieId, ct);
                var omieLastAlt = OmieTimestampHelper.ParseOmieDateTime(omieProduto.DAlt, omieProduto.HAlt);

                if (existing == null)
                {
                    var novoProduto = new Produto
                    {
                        Id = Guid.NewGuid(),
                        OmieId = omieId,
                        CodigoProduto = omieProduto.Codigo,
                        Descricao = omieProduto.Descricao,
                        PrecoUnitario = omieProduto.ValorUnitario,
                        Ncm = omieProduto.Ncm,
                        UnidadeMedida = omieProduto.Unidade,
                        PesoLiquido = omieProduto.PesoLiquido,
                        PesoBruto = omieProduto.PesoBruto,
                        FamiliaProduto = omieProduto.FamiliaProduto,
                        Ativo = omieProduto.Inativo == "N",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        OmieUpdatedAt = omieLastAlt
                    };
                    _dbContext.Produtos.Add(novoProduto);
                }
                else
                {
                    if (existing.OmieUpdatedAt.HasValue && omieLastAlt.HasValue &&
                        existing.OmieUpdatedAt.Value == omieLastAlt.Value)
                    {
                        _logger.LogDebug("Produto OmieId {OmieId} já está atualizado. Pulando UPDATE.", omieId);
                        return;
                    }

                    existing.CodigoProduto = omieProduto.Codigo;
                    existing.Descricao = omieProduto.Descricao;
                    existing.PrecoUnitario = omieProduto.ValorUnitario;
                    existing.Ncm = omieProduto.Ncm;
                    existing.UnidadeMedida = omieProduto.Unidade;
                    existing.PesoLiquido = omieProduto.PesoLiquido;
                    existing.PesoBruto = omieProduto.PesoBruto;
                    existing.FamiliaProduto = omieProduto.FamiliaProduto;
                    existing.Ativo = omieProduto.Inativo == "N";
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.OmieUpdatedAt = omieLastAlt;
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Produto OmieId {OmieId} sincronizado individualmente com sucesso.", omieId);
            }
            else
            {
                _logger.LogWarning("Produto OmieId {OmieId} não encontrado na Omie para consulta individual.", omieId);
            }
        }
    }
}
