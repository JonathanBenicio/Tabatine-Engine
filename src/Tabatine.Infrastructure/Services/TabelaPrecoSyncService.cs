using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models.Produtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class TabelaPrecoSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<TabelaPrecoSyncService> _logger;

        public TabelaPrecoSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<TabelaPrecoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Tabelas de Preços...");

            int pagina = 1;
            bool temMais = true;

            while (temMais)
            {
                var response = await _omieClient.ListarTabelasPrecoAsync(pagina, ct);

                if (response?.ListaTabelasPreco == null || !response.ListaTabelasPreco.Any())
                {
                    break;
                }

                var omieIds = response.ListaTabelasPreco.Select(t => t.CodigoTabelaPreco).ToList();
                var existingTabelas = await _dbContext.TabelasPreco
                    .Where(t => omieIds.Contains(t.OmieId))
                    .ToDictionaryAsync(t => t.OmieId, ct);

                foreach (var omieTab in response.ListaTabelasPreco)
                {
                    TabelaPreco current;
                    if (existingTabelas.TryGetValue(omieTab.CodigoTabelaPreco, out var existing))
                    {
                        existing.Nome = omieTab.Nome;
                        existing.Codigo = omieTab.Codigo;
                        existing.Ativa = omieTab.Ativa == "S";
                        existing.UpdatedAt = DateTime.UtcNow;
                        current = existing;
                    }
                    else
                    {
                        current = new TabelaPreco
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieTab.CodigoTabelaPreco,
                            Nome = omieTab.Nome,
                            Codigo = omieTab.Codigo,
                            Ativa = omieTab.Ativa == "S",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _dbContext.TabelasPreco.Add(current);
                    }

                    // Sincronizar Itens da Tabela
                    await SyncItensDaTabelaAsync(current, ct);
                }

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Página {Pagina} de Tabelas de Preços sincronizada.", pagina);

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }

            _logger.LogInformation("Sincronização de Tabelas de Preços concluída.");
        }

        private async Task SyncItensDaTabelaAsync(TabelaPreco tabela, CancellationToken ct)
        {
            int pagina = 1;
            bool temMais = true;

            // Cache de produtos para evitar múltiplas consultas
            var produtos = await _dbContext.Produtos.ToDictionaryAsync(p => p.OmieId, p => p.Id, ct);

            while (temMais)
            {
                var response = await _omieClient.ListarTabelaItensAsync(tabela.OmieId, pagina, ct);

                if (response?.ListaTabelaPreco == null || !response.ListaTabelaPreco.Any())
                {
                    break;
                }

                // A Omie retorna uma lista de objetos que contêm a lista de itens (estranho, mas mapeado)
                var omieItens = response.ListaTabelaPreco.SelectMany(w => w.ItensTabela).ToList();
                
                var omieProdIds = omieItens.Select(i => i.CodigoProduto).ToList();
                var existingItens = await _dbContext.TabelaPrecoItens
                    .Where(i => i.TabelaPrecoId == tabela.Id && omieProdIds.Contains(_dbContext.Produtos.First(p => p.Id == i.ProdutoId).OmieId))
                    .ToListAsync(ct);

                foreach (var omieItem in omieItens)
                {
                    if (produtos.TryGetValue(omieItem.CodigoProduto, out var produtoId))
                    {
                        var existing = existingItens.FirstOrDefault(i => i.ProdutoId == produtoId);
                        if (existing != null)
                        {
                            existing.Valor = omieItem.ValorTabela;
                        }
                        else
                        {
                            _dbContext.TabelaPrecoItens.Add(new TabelaPrecoItem
                            {
                                Id = Guid.NewGuid(),
                                TabelaPrecoId = tabela.Id,
                                ProdutoId = produtoId,
                                Valor = omieItem.ValorTabela
                            });
                        }
                    }
                }

                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }
        }
    }
}
