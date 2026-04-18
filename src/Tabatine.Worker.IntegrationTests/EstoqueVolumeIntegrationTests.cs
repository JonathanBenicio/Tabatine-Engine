using Tabatine.Omie.Client.Models.Estoque;

namespace Tabatine.Worker.IntegrationTests;

/// <summary>
/// Teste de volume do streaming de ProdutoEstoque com IAsyncEnumerable.
/// Valida que a sincronização de 1000+ registros não estoura memória nem perde dados.
/// Sprint 2 — Issue #36 (filho de #25)
/// </summary>
public class EstoqueVolumeIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Sincronizar_1000_Registros_De_Estoque_Sem_Perda_De_Dados()
    {
        // Arrange — gerar 1000 produtos e respectivos saldos de estoque
        const int totalRegistros = 1000;
        var omieIdLocalEstoque = 7777L;

        // Pré-popula o local de estoque
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await dbContext.LocaisEstoque.AnyAsync(l => l.OmieId == omieIdLocalEstoque))
            {
                dbContext.LocaisEstoque.Add(new LocalEstoque
                {
                    Id = Guid.NewGuid(),
                    OmieId = omieIdLocalEstoque,
                    Codigo = "LOC-VOL",
                    Descricao = "Almoxarifado Volume Test",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Garante que os 1000 produtos existam no banco, senão o estoque é ignorado
            var countInDB = await dbContext.Produtos.CountAsync(p => p.OmieId >= 900001 && p.OmieId <= 900000 + totalRegistros);
            if (countInDB < totalRegistros)
            {
                var produtosParaAdd = Enumerable.Range(1, totalRegistros)
                    .Select(i => new Produto
                    {
                        Id = Guid.NewGuid(),
                        OmieId = 900_000L + i,
                        CodigoProduto = $"VOL-{i:D5}",
                        Descricao = $"Produto Volume {i}",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                
                await dbContext.Produtos.AddRangeAsync(produtosParaAdd);
            }

            await dbContext.SaveChangesAsync();
        }

        // Mock de streaming: gera 1000 registros
        var saldosMocados = Enumerable.Range(1, totalRegistros)
            .Select(i => new ProdutoEstoqueDto
            {
                CodProd = 900_000L + i,
                CodigoLocalEstoque = omieIdLocalEstoque,
                Saldo = i * 1.5m,
                Fisico = i * 1.5m,
                Cmc = 10.0m
            })
            .ToList();

        Factory.OmieClientMock
            .StreamPosicaoEstoqueAsync(Arg.Any<ListarPosEstoqueRequest>(), Arg.Any<CancellationToken>())
            .Returns(saldosMocados.ToAsyncEnumerable());

        // Também precisamos mockar os produtos em si (sem produtos no catálogo, estoque é ignorado)
        Factory.OmieClientMock
            .ListarProdutosAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new Tabatine.Omie.Client.Models.Produtos.ListarProdutosResponse
            {
                Pagina = 1,
                TotalDePaginas = 1,
                ProdutosCadastro = Enumerable.Range(1, totalRegistros)
                    .Select(i => new Tabatine.Omie.Client.Models.Produtos.OmieProduto
                    {
                        CodigoProduto = 900_000L + i,
                        Descricao = $"Produto Volume {i}",
                        Codigo = $"VOL-{i:D5}"
                    })
                    .ToList()
            });

        // Act — sincronização de Estoque via SyncService
        using (var syncScope = Factory.Services.CreateScope())
        {
            var estoqueSvc = syncScope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.EstoqueSyncService>();
            await estoqueSvc.SyncAllAsync();
        }

        // Assert — deve existir exatamente 1000 registros de saldo de estoque
        using (var assertScope = Factory.Services.CreateScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var totalPersistido = await dbContext.ProdutosEstoque.CountAsync();

            Assert.Equal(totalRegistros, totalPersistido);
        }
    }

    [Fact]
    public async Task Deve_Atualizar_Saldo_Existente_Sem_Duplicar_Ao_Resincronizar()
    {
        // Idempotência com volume reduzido: mesmo produto sincronizado 2x não duplica
        var codigoProdutoOmie = 950_001L;
        var omieIdLocal = 8888L;

        using (var prep = Factory.Services.CreateScope())
        {
            var dbContext = prep.ServiceProvider.GetRequiredService<AppDbContext>();

            if (!await dbContext.LocaisEstoque.AnyAsync(l => l.OmieId == omieIdLocal))
            {
                dbContext.LocaisEstoque.Add(new LocalEstoque
                {
                    Id = Guid.NewGuid(), 
                    OmieId = omieIdLocal, 
                    Codigo = "LOC-IDEM",
                    Descricao = "Local Idempotente",
                    CreatedAt = DateTime.UtcNow, 
                    UpdatedAt = DateTime.UtcNow
                });
            }

            if (!await dbContext.Produtos.AnyAsync(p => p.OmieId == codigoProdutoOmie))
            {
                dbContext.Produtos.Add(new Produto
                {
                    Id = Guid.NewGuid(), OmieId = codigoProdutoOmie,
                    CodigoProduto = "IDEM-001", Descricao = "Produto Idempotência",
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
        }

        var mockEstoque = new List<ProdutoEstoqueDto>
        {
            new() { CodProd = codigoProdutoOmie, CodigoLocalEstoque = omieIdLocal, Saldo = 50m, Fisico = 50m }
        };

        Factory.OmieClientMock.StreamPosicaoEstoqueAsync(Arg.Any<ListarPosEstoqueRequest>(), Arg.Any<CancellationToken>())
            .Returns(mockEstoque.ToAsyncEnumerable());

        // Sync 2x
        for (var i = 0; i < 2; i++)
        {
            using var scope = Factory.Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.EstoqueSyncService>();
            await svc.SyncAllAsync();
        }

        using (var assertScope = Factory.Services.CreateScope())
        {
            var dbContext = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await dbContext.ProdutosEstoque
                .CountAsync(e => e.Produto != null && e.Produto.OmieId == codigoProdutoOmie);

            Assert.Equal(1, count);
        }
    }
}
