using Tabatine.Omie.Client.Models.Estoque;
using Tabatine.Omie.Client.Models.Produtos;

namespace Tabatine.Worker.IntegrationTests;

/// <summary>
/// Cenários de borda para Estoque e Catálogo de Produtos.
/// Épico #29: Auditoria de Testes — Catálogo e Estoque.
/// </summary>
public class EstoqueEdgeCasesTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Lidar_Com_Produto_Com_Codigo_Invalido_Ou_Duplicado()
    {
        // Arrange
        var omieIdValido = 111222L;
        var omieIdDuplicado = 111222L; // Mesmo ID simulando inconsistência de payload ou duplicidade
        
        var produtos = new List<OmieProduto>
        {
            new() { CodigoProduto = omieIdValido, Descricao = "Produto Válido", Codigo = "PROD-VAL" },
            new() { CodigoProduto = omieIdDuplicado, Descricao = "Produto Duplicado", Codigo = "PROD-DUP" }
        };

        Factory.OmieClientMock.ListarProdutosAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new ListarProdutosResponse
            {
                Pagina = 1,
                TotalDePaginas = 1,
                ProdutosCadastro = produtos
            });

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ProdutoSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert — deve existir apenas 1 registro no banco (idempotência por OmieId)
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await dbContext.Produtos.CountAsync(p => p.OmieId == omieIdValido);
            
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task Deve_Processar_Movimentacao_De_Estoque_Negativa()
    {
        // No Omie, saldo negativo pode ocorrer dependendo da configuração.
        // O sistema deve persistir o valor conforme retornado.
        var omieIdProd = 333444L;
        var omieIdLocal = 1010L;

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            dbContext.LocaisEstoque.Add(new LocalEstoque { Id = Guid.NewGuid(), OmieId = omieIdLocal, Codigo = "LOC-NEG", Descricao = "Local Negativo" });
            dbContext.Produtos.Add(new Produto { Id = Guid.NewGuid(), OmieId = omieIdProd, CodigoProduto = "NEG-001", Descricao = "Produto Teste Negativo" });
            await dbContext.SaveChangesAsync();
        }

        var mockEstoque = new List<ProdutoEstoqueDto>
        {
            new() { CodProd = omieIdProd, CodigoLocalEstoque = omieIdLocal, Saldo = -15.5m, Fisico = -15.5m }
        };

        Factory.OmieClientMock.StreamPosicaoEstoqueAsync(Arg.Any<ListarPosEstoqueRequest>(), Arg.Any<CancellationToken>())
            .Returns(mockEstoque.ToAsyncEnumerable());

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.EstoqueSyncService>();
            await svc.SyncAllAsync();
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var estoque = await dbContext.ProdutosEstoque.FirstOrDefaultAsync(e => e.Produto.OmieId == omieIdProd);

            Assert.NotNull(estoque);
            Assert.Equal(-15.5m, estoque!.Saldo);
        }
    }
}
