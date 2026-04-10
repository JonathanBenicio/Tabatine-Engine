using Tabatine.Omie.Client.Models.Vendedores;

namespace Tabatine.Worker.IntegrationTests;

public class VendedorWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_Vendedor_E_Persistir_No_Banco()
    {
        // Arrange
        var omieIdVendedor = 778899L;
        
        // 1. Mock da resposta da Omie para consulta individual de vendedor
        var omieVendedor = new OmieVendedor
        {
            Codigo = omieIdVendedor,
            Nome = "Vendedor Teste Webhook",
            Email = "vendedor@teste.com",
            Comissao = 5.5m,
            Inativo = "N"
        };

        Factory.OmieClientMock.ConsultarVendedorAsync(omieIdVendedor, Arg.Any<CancellationToken>())
            .Returns(omieVendedor);

        // 2. Payload do Webhook
        var webhookEvent = new
        {
            topic = "Vendedor.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                codigo = omieIdVendedor,
                nome = "Vendedor Teste Webhook"
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        // Polling
        Vendedor? vendedorDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            vendedorDB = await dbContext.Vendedores.FirstOrDefaultAsync(v => v.OmieId == omieIdVendedor);
            
            if (vendedorDB != null) break;
            await Task.Delay(500);
        }

        vendedorDB.Should().NotBeNull("O Vendedor deve ser persistido no banco de dados");
        vendedorDB!.Nome.Should().Be("Vendedor Teste Webhook");
        vendedorDB.Email.Should().Be("vendedor@teste.com");
        vendedorDB.Comissao.Should().Be(5.5m);
    }
}
