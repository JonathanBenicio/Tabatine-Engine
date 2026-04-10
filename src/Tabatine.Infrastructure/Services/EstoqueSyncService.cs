using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models.Estoque;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Infrastructure.Services
{
    public class EstoqueSyncService : ISyncService
    {
        private readonly IOmieClient _omieClient;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<EstoqueSyncService> _logger;

        public EstoqueSyncService(
            IOmieClient omieClient, 
            AppDbContext dbContext, 
            ILogger<EstoqueSyncService> logger)
        {
            _omieClient = omieClient;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando sincronização completa de Estoque...");

            await SyncLocaisAsync(ct);
            await SyncSaldosAsync(ct);

            _logger.LogInformation("Sincronização de Estoque finalizada.");
        }

        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            // O padrão do SyncByIdAsync no EstoqueSyncService é sincronizar saldo de produto.
            await SyncProdutoSaldoAsync(omieId, ct);
        }

        public async Task SyncLocalByIdAsync(long localId, CancellationToken ct = default)
        {
            _logger.LogInformation("Sincronizando Local de Estoque individual. OmieId: {LocalId}", localId);
            
            // Omie não tem consulta individual de local por ID numérico direto no "obter", 
            // mas podemos usar a listagem filtrada se existir ou simplesmente rodar o SyncLocaisAsync que é leve.
            // Para ser 100% preciso e seguir o padrão, rodamos o SyncLocaisAsync filtrado ou completo.
            // Como são poucos locais, SyncLocaisAsync(ct) é suficiente e seguro.
            await SyncLocaisAsync(ct);
        }

        private async Task SyncProdutoSaldoAsync(long omieId, CancellationToken ct)
        {
            _logger.LogInformation("Sincronizando saldo específico para Produto OmieId: {OmieId}", omieId);
            
            // Busca o produto no banco local
            var produto = await _dbContext.Produtos.FirstOrDefaultAsync(p => p.OmieId == omieId, ct);
            if (produto == null)
            {
                _logger.LogWarning("Produto OmieId {OmieId} não encontrado no banco local. Sincronize o produto primeiro.", omieId);
                return;
            }

            // Consulta resumo na Omie
            var request = new ObterEstoqueProdutoRequest { IdProduto = omieId };
            var response = await _omieClient.ObterResumoEstoqueProdutoAsync(request, ct);

            if (response != null && response.ListaEstoque != null)
            {
                var locaisCache = await _dbContext.LocaisEstoque.ToDictionaryAsync(l => l.OmieId, ct);

                foreach (var item in response.ListaEstoque)
                {
                    if (!locaisCache.TryGetValue(item.IdLocal, out var local))
                    {
                        _logger.LogWarning("Local de Estoque OmieId {LocalId} não encontrado localmente. Ignorando saldo.", item.IdLocal);
                        continue;
                    }

                    await UpsertSaldoAsync(produto.Id, local.Id, new ProdutoEstoqueDto
                    {
                        Saldo = item.Disponivel,
                        Reservado = item.Reservado,
                        Pendente = item.PrevisaoSaida,
                        Fisico = item.Fisico,
                        Cmc = item.Cmc,
                        EstoqueMinimo = item.EstoqueMinimo
                    }, ct);
                }

                await _dbContext.SaveChangesAsync(ct);
            }
        }

        private async Task SyncLocaisAsync(CancellationToken ct)
        {
            _logger.LogInformation("Sincronizando Locais de Estoque...");
            
            var existingLocais = await _dbContext.LocaisEstoque.ToDictionaryAsync(l => l.OmieId, ct);

            await foreach (var item in _omieClient.ListarLocaisEstoqueAsync(ct))
            {
                if (existingLocais.TryGetValue(item.CodigoLocalEstoque, out var existing))
                {
                    existing.Codigo = item.Codigo ?? existing.Codigo;
                    existing.Descricao = item.Descricao ?? existing.Descricao;
                    existing.Tipo = item.Tipo;
                    existing.Padrao = item.Padrao == "S";
                    existing.Inativo = item.Inativo == "S";
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var novo = new LocalEstoque
                    {
                        Id = Guid.NewGuid(),
                        OmieId = item.CodigoLocalEstoque,
                        Codigo = item.Codigo ?? string.Empty,
                        Descricao = item.Descricao ?? string.Empty,
                        Tipo = item.Tipo,
                        Padrao = item.Padrao == "S",
                        Inativo = item.Inativo == "S",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _dbContext.LocaisEstoque.Add(novo);
                    existingLocais[novo.OmieId] = novo;
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Sincronização de Locais de Estoque finalizada.");
        }

        private async Task SyncSaldosAsync(CancellationToken ct)
        {
            _logger.LogInformation("Sincronizando Saldos de Estoque (Streaming)...");

            // Cache de IDs para evitar múltiplas consultas ao banco dentro do loop
            var produtosCache = await _dbContext.Produtos.Select(p => new { p.Id, p.OmieId }).ToDictionaryAsync(p => p.OmieId, p => p.Id, ct);
            var locaisCache = await _dbContext.LocaisEstoque.Select(l => new { l.Id, l.OmieId }).ToDictionaryAsync(l => l.OmieId, l => l.Id, ct);

            var request = new ListarPosEstoqueRequest 
            { 
                Pagina = 1, 
                RegPorPagina = 100,
                ExibeTodos = "S" 
            };

            int count = 0;
            await foreach (var item in _omieClient.StreamPosicaoEstoqueAsync(request, ct))
            {
                if (!produtosCache.TryGetValue(item.CodProd, out var produtoId))
                {
                    _logger.LogDebug("Produto OmieId {OmieId} não encontrado no banco local. Pulando saldo.", item.CodProd);
                    continue;
                }

                if (!locaisCache.TryGetValue(item.CodigoLocalEstoque, out var localId))
                {
                    _logger.LogDebug("Local OmieId {LocalId} não encontrado no banco local. Pulando saldo.", item.CodigoLocalEstoque);
                    continue;
                }

                await UpsertSaldoAsync(produtoId, localId, item, ct);
                
                count++;
                if (count % 500 == 0)
                {
                    await _dbContext.SaveChangesAsync(ct);
                    _logger.LogInformation("{Count} saldos processados...", count);
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Total de {Count} registros de saldo processados.", count);
        }

        private async Task UpsertSaldoAsync(Guid produtoId, Guid localId, ProdutoEstoqueDto dto, CancellationToken ct)
        {
            var existing = await _dbContext.ProdutosEstoque
                .FirstOrDefaultAsync(se => se.ProdutoId == produtoId && se.LocalEstoqueId == localId, ct);

            if (existing != null)
            {
                existing.Saldo = dto.Saldo;
                existing.Reservado = dto.Reservado;
                existing.Pendente = dto.Pendente;
                existing.Fisico = dto.Fisico;
                existing.Cmc = dto.Cmc;
                existing.EstoqueMinimo = dto.EstoqueMinimo;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var novo = new ProdutoEstoque
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = produtoId,
                    LocalEstoqueId = localId,
                    Saldo = dto.Saldo,
                    Reservado = dto.Reservado,
                    Pendente = dto.Pendente,
                    Fisico = dto.Fisico,
                    Cmc = dto.Cmc,
                    EstoqueMinimo = dto.EstoqueMinimo,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.ProdutosEstoque.Add(novo);
            }
        }

        public Task CancelByIdAsync(long omieId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
