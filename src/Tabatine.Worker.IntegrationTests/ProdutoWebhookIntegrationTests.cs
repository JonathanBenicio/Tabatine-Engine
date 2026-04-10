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
            @event = new { idProduto = omieId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", payload);

        // Assert - Resposta imediata do Endpoint
        response.EnsureSuccessStatusCode();

        // Assert - Validação do processamento assíncrono (Polling)
        Produto? produtoPersistido = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            produtoPersistido = await db.Produtos.FirstOrDefaultAsync(p => p.OmieId == omieId);
            if (produtoPersistido != null) break;
            
            await Task.Delay(500);
        }

        // Verificações Finais
        produtoPersistido.Should().NotBeNull("O Produto deveria ter sido persistido pelo processador de webhooks.");
        produtoPersistido!.Descricao.Should().Be(produtoOmie.Descricao);
        produtoPersistido.CodigoProduto.Should().Be(produtoOmie.Codigo);
        produtoPersistido.PrecoUnitario.Should().Be(produtoOmie.ValorUnitario);
        
        // Verificar se a notificação foi "enviada" (Mock acionado)
        await Factory.NotificationServiceMock.ReceivedWithAnyArgs(1)
            .SendNotificationAsync(Arg.Any<string>(), Arg.Any<string>(), "PRODUTO", omieId);
    }
}
