namespace Tabatine.Worker.IntegrationTests.Webhooks;

public class ProdutoWebhookIntegrationTests : BaseIntegrationTest
{
    public ProdutoWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Deve_Processar_Webhook_Produto_E_Persistir_No_Banco_Com_Sucesso()
    {
        // Arrange
        var omieId = 987654321L;
        
        // Mock do retorno da Omie (ConsultarProdutoAsync)
        var produtoOmie = new OmieProduto
        {
            CodigoProduto = omieId,
            Codigo = "PRD-TESTE-001",
            Descricao = "Produto de Teste Integrado",
            ValorUnitario = 150.50m,
            Inativo = "N",
            DAlt = "09/04/2026",
            HAlt = "12:00:00"
        };
        
        Factory.OmieClientMock.ConsultarProdutoAsync(omieId, Arg.Any<CancellationToken>())
            .Returns(produtoOmie);

        // Payload simulando Omie Connect 2.0
        var payload = new
        {
            appKey = "teste-key",
            topic = "Produto.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new { nCodProd = omieId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", payload);
        response.EnsureSuccessStatusCode();

        // Aguarda o worker processar a fila
        await WaitForWebhookQueueToDrainAsync(TimeSpan.FromSeconds(10));

        // Assert
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var produtoPersistido = await db.Produtos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OmieId == omieId);

        // Verificações Finais
        produtoPersistido.Should().NotBeNull("O Produto deveria ter sido persistido pelo processador de webhooks.");
        produtoPersistido!.Descricao.Should().Be(produtoOmie.Descricao);
        produtoPersistido.CodigoProduto.Should().Be(produtoOmie.Codigo);
        produtoPersistido.PrecoUnitario.Should().Be(produtoOmie.ValorUnitario);
        
        // Verificar se a notificação foi "enviada" (Mock acionado)
        await Factory.NotificationServiceMock.ReceivedWithAnyArgs(1)
            .SendNotificationAsync(Arg.Any<string>(), Arg.Any<string>(), "PRODUTO", omieId);
    }

    [Fact]
    public async Task Deve_Tratar_Produto_Com_Codigo_Duplicado_E_Atualizar_Existente()
    {
        var omieId = 111222333L;
        var codigoProduto = "PRD-DUPLICADO";

        // Pre-popula o banco
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var produtoAntigo = new Produto
            {
                Id = Guid.NewGuid(),
                OmieId = omieId,
                CodigoProduto = codigoProduto,
                Descricao = "Descricao Antiga",
                PrecoUnitario = 100.00m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Produtos.Add(produtoAntigo);
            await db.SaveChangesAsync();
        }

        var produtoOmie = new OmieProduto
        {
            CodigoProduto = omieId,
            Codigo = codigoProduto,
            Descricao = "Descricao Nova Atualizada",
            ValorUnitario = 200.00m,
            Inativo = "N",
            DAlt = "10/04/2026",
            HAlt = "12:00:00"
        };
        
        Factory.OmieClientMock.ConsultarProdutoAsync(omieId, Arg.Any<CancellationToken>())
            .Returns(produtoOmie);

        var payload = new
        {
            appKey = "teste-key",
            topic = "Produto.Alterado",
            messageId = Guid.NewGuid().ToString(),
            @event = new { nCodProd = omieId }
        };

        var response = await Client.PostAsJsonAsync("/webhook/omie", payload);
        response.EnsureSuccessStatusCode();

        Produto? produtoPersistido = null;
        var timeout = TimeSpan.FromSeconds(20);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            produtoPersistido = await db.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OmieId == omieId);

            if (produtoPersistido != null && produtoPersistido.Descricao == "Descricao Nova Atualizada") break;
            
            await Task.Delay(500);
        }

        produtoPersistido.Should().NotBeNull();
        produtoPersistido!.Descricao.Should().Be("Descricao Nova Atualizada");
        produtoPersistido.PrecoUnitario.Should().Be(200.00m);
    }
}
