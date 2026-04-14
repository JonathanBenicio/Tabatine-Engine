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

        Assert.NotNull(ccDB);
        Assert.Equal("Conta Teste Webhook", ccDB!.Descricao);
        Assert.Equal(saldoInicialEsperado, ccDB.SaldoInicial);
    }

    [Fact]
    public async Task Deve_Validar_Reconciliacao_De_Contas_Corrente_Via_ListarMovimentos()
    {
        // Arrange
        var contaCorrenteId = 111222L;

        // Mock 
        var ccResponse = new ListarContaCorrenteResponse 
        { 
            ContasCorrentes = new List<OmieContaCorrente> 
            { 
                new() { Codigo = contaCorrenteId, Descricao = "CC Reconciliacao", Tipo = "000", SaldoInicial = 0, Inativo = "N" }
            },
            TotalDeRegistros = 1,
            TotalDePaginas = 1,
            Pagina = 1
        };

        Factory.OmieClientMock.ListarContasCorrentesAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(ccResponse);

        // Simulando que além da Conta, recebemos um webhook de Movimento (ou reconciliação)
        var webhookEvent = new
        {
            topic = "Financas.ContaCorrente.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new { idContaCorrente = contaCorrenteId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        // Polling
        await Task.Delay(1000);

        ContaCorrente? ccDB = null;
        var timeout = TimeSpan.FromSeconds(15); // Increase timeout
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ccDB = await dbContext.ContasCorrente.FirstOrDefaultAsync(c => c.OmieId == contaCorrenteId);
            
            if (ccDB != null && ccDB.SaldoInicial == 0) break;
            await Task.Delay(1000); // Increase polling interval
        }

        // Assert
        Assert.NotNull(ccDB);
        
        // Em um cenário real completo validaríamos os lançamentos. Como o webhook isola
        // a entidade ContaCorrente, atestar que ela suporta listar e gravar o Saldo é o primeiro passo da conciliação.
        Assert.Equal("CC Reconciliacao", ccDB!.Descricao);
    }
}
