using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
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
        private readonly ILogger<ProdutoSyncService> _logger;

        public ProdutoSyncService(IOmieClient omieClient, AppDbContext dbContext, ILogger<ProdutoSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização de Produtos...");
            int pagina = 1;
            bool temMais = true;

            while (temMais && !ct.IsCancellationRequested)
            {
                var response = await _omieClient.ListarProdutosAsync(pagina, ct);
                if (response.ProdutosCadastro.Count == 0) break;

                foreach (var omieItem in response.ProdutosCadastro)
                {
                    var existing = await _dbContext.Produtos
                        .FirstOrDefaultAsync(p => p.OmieId == omieItem.CodigoProduto, ct);

                    if (existing == null)
                    {
                        _dbContext.Produtos.Add(new Produto
                        {
                            Id = Guid.NewGuid(),
                            OmieId = omieItem.CodigoProduto,
                            CodigoProduto = omieItem.Codigo,
                            Descricao = omieItem.Descricao,
                            PrecoUnitario = omieItem.ValorUnitario,
                            Ncm = omieItem.Ncm,
                            Ativo = omieItem.Inativo == "N",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existing.CodigoProduto = omieItem.Codigo;
                        existing.Descricao = omieItem.Descricao;
                        existing.PrecoUnitario = omieItem.ValorUnitario;
                        existing.Ncm = omieItem.Ncm;
                        existing.Ativo = omieItem.Inativo == "N";
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                temMais = pagina < response.TotalDePaginas;
                pagina++;
            }
            _logger.LogInformation("Sincronização de Produtos finalizada.");
        }
    }
}
