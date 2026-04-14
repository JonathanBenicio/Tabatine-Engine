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

        // Aguarda o worker processar a fila com polling robusto
        Produto? produtoPersistido = null;
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scopeLoop = Factory.Services.CreateScope();
            var dbLoop = scopeLoop.ServiceProvider.GetRequiredService<AppDbContext>();
            
            produtoPersistido = await dbLoop.Produtos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OmieId == omieId);

            if (produtoPersistido != null) break;
            
            await Task.Delay(500);
        }

        if (produtoPersistido == null)
        {
            using var diagScope = Factory.Services.CreateScope();
            var dbDiag = diagScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var webhook = await dbDiag.WebhookEvents
                .AsNoTracking()
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(w => w.Event == "Produto.Incluido");

            Assert.Fail($"Produto {omieId} não persistido após {timeout.TotalSeconds}s. " +
                        $"Webhook status: {(webhook == null ? "Não encontrado" : (webhook.ProcessedAt.HasValue ? $"Processado em {webhook.ProcessedAt}" : $"Pendente/Erro (ID: {webhook.Id})"))}");
        }

        // Verificações Finais
        Assert.NotNull(produtoPersistido);
        Assert.Equal(produtoOmie.Descricao, produtoPersistido!.Descricao);
        Assert.Equal(produtoOmie.Codigo, produtoPersistido.CodigoProduto);
        Assert.Equal(produtoOmie.ValorUnitario, produtoPersistido.PrecoUnitario);
        
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
        var timeout = TimeSpan.FromSeconds(30);
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

        if (produtoPersistido == null || produtoPersistido.Descricao != "Descricao Nova Atualizada")
        {
            using var diagScope = Factory.Services.CreateScope();
            var dbDiag = diagScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var webhook = await dbDiag.WebhookEvents
                .AsNoTracking()
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(w => w.Event == "Produto.Alterado");

            Assert.Fail($"Produto {omieId} não atualizado após {timeout.TotalSeconds}s. " +
                        $"Webhook status: {(webhook == null ? "Não encontrado" : (webhook.ProcessedAt.HasValue ? $"Processado em {webhook.ProcessedAt}" : $"Pendente/Erro (ID: {webhook.Id})"))}");
        }

        Assert.NotNull(produtoPersistido);
        Assert.Equal("Descricao Nova Atualizada", produtoPersistido!.Descricao);
        Assert.Equal(200.00m, produtoPersistido.PrecoUnitario);
    }
}
