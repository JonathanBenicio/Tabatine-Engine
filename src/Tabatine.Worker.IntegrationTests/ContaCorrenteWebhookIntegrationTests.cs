using Tabatine.Omie.Client.Models.ContaCorrente;

namespace Tabatine.Worker.IntegrationTests;

public class ContaCorrenteWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_ContaCorrente_E_Atualizar_Saldo_Inicial()
    {
        // Arrange
        var omieIdCC = 556677L;
        var saldoInicialEsperado = 12500.75m;

        // 1. Mock da resposta da Omie para ListarContasCorrentes (já que não há Consultar individual)
        // O SyncByIdAsync agora usará a listagem para encontrar a conta
        var omieCC = new OmieContaCorrente
        {
            Codigo = omieIdCC,
            Descricao = "Conta Teste Webhook",
            Tipo = "000", // Corrente
            SaldoInicial = saldoInicialEsperado,
            Inativo = "N"
        };

        var responseMock = new ListarContaCorrenteResponse 
        { 
            ContasCorrentes = new List<OmieContaCorrente> { omieCC },
            TotalDeRegistros = 1,
            TotalDePaginas = 1,
            Pagina = 1
        };

        Factory.OmieClientMock.ListarContasCorrentesAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(responseMock);

        // 2. Payload do Webhook
        var webhookEvent = new
        {
            topic = "Financas.ContaCorrente.Alterado",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                idContaCorrente = omieIdCC
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        // Polling
        ContaCorrente? ccDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ccDB = await dbContext.ContasCorrente.FirstOrDefaultAsync(c => c.OmieId == omieIdCC);
            
            if (ccDB != null && ccDB.SaldoInicial == saldoInicialEsperado) break;
            await Task.Delay(500);
        }

        ccDB.Should().NotBeNull("A Conta Corrente deve ser persistida/atualizada");
        ccDB!.Descricao.Should().Be("Conta Teste Webhook");
        ccDB.SaldoInicial.Should().Be(saldoInicialEsperado);
    }
}
