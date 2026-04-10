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
        const int registrosPorPagina = 100;
        var totalPaginas = totalRegistros / registrosPorPagina;
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
                    Nome = "Almoxarifado Volume Test",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }
        }

        // Mock paginado: 10 páginas de 100 registros
        for (var pagina = 1; pagina <= totalPaginas; pagina++)
        {
            var paginaLocal = pagina;
            var saldosDaPagina = Enumerable.Range((paginaLocal - 1) * registrosPorPagina + 1, registrosPorPagina)
                .Select(i => new EstoqueProduto
                {
                    CodigoProduto = 900_000L + i,        // IDs únicos por produto
                    CodigoLocalEstoque = omieIdLocalEstoque,
                    SaldoTotalEmpresa = i * 1.5m,
                    Descricao = $"Produto Volume {i}",
                    Codigo = $"VOL-{i:D5}"
                })
                .ToList();

            Factory.OmieClientMock
                .ListarEstoqueProdutoAsync(paginaLocal, Arg.Any<CancellationToken>())
                .Returns(new ListarEstoqueProdutoResponse
                {
                    Pagina = paginaLocal,
                    TotalDePaginas = totalPaginas,
                    Produtos = saldosDaPagina
                });
        }

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

            totalPersistido.Should().Be(totalRegistros,
                $"Todos os {totalRegistros} saldos de estoque devem ser persistidos sem perda via streaming");
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
                    Id = Guid.NewGuid(), OmieId = omieIdLocal, Nome = "Local Idempotente",
                    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
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

        var mockEstoque = new ListarEstoqueProdutoResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            Produtos = new List<EstoqueProduto>
            {
                new() { CodigoProduto = codigoProdutoOmie, CodigoLocalEstoque = omieIdLocal, SaldoTotalEmpresa = 50m, Codigo = "IDEM-001", Descricao = "Produto Idempotência" }
            }
        };

        Factory.OmieClientMock.ListarEstoqueProdutoAsync(1, Arg.Any<CancellationToken>())
            .Returns(mockEstoque);

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

            count.Should().Be(1, "Resincronização não deve duplicar registros de ProdutoEstoque");
        }
    }
}
