using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net.Http.Json;
using Tabatine.Core.Entities;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client.Models.Bancos;

namespace Tabatine.Worker.IntegrationTests.Webhooks;

public class SystemManualSyncWebhookIntegrationTests : BaseIntegrationTest
{
    public SystemManualSyncWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Deve_Processar_Webhook_ManualSync_E_Executar_Rotina_De_Sincronizacao()
    {
        // 1. Arrange - Preparar mock do OmieClient
        // Como o SyncManager executa Bancos primeiro, mockar a resposta de Bancos é o mais rápido
        var omieBanco = new OmieBanco
        {
            Codigo = "999",
            Nome = "Banco De Teste Manual Sync",
            Tipo = "B",
            CodigoIspb = "12345678"
        };

        var responseMock = new ListarBancosResponse
        {
            Pagina = 1,
            TotalDePaginas = 1,
            Bancos = new List<OmieBanco> { omieBanco }
        };

        Factory.OmieClientMock.ListarBancosAsync(1, Arg.Any<CancellationToken>())
            .Returns(responseMock);

        // 2. Criar Payload do Webhook (Padrão Connect 2.0 / Padrão Interno Tabatine)
        var webhookEvent = new
        {
            topic = "System.ManualSync",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                reason = "Teste Integrado de Sincronização Manual"
            }
        };

        // 3. Act - Disparar o Webhook para Ingestion
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert - Resposta imediata 200 OK do endpoint
        response.EnsureSuccessStatusCode();

        // 4. Assert - Validação do processamento assíncrono (Polling)
        Banco? bancoPersistido = null;
        var timeout = TimeSpan.FromSeconds(15); 
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // O SyncManager iterará as rotinas. Bancos é a primeira, então validamos que ela foi chamada
            bancoPersistido = await db.Bancos.FirstOrDefaultAsync(b => b.CodigoBanco == "999");
            if (bancoPersistido != null) break;
            
            await Task.Delay(500);
        }

        // 5. Verificações Finais
        Assert.NotNull(bancoPersistido);
        Assert.Equal(omieBanco.Nome, bancoPersistido!.Nome);
        Assert.Equal(omieBanco.CodigoIspb, bancoPersistido.CodigoIspb);
    }
}
