using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Infrastructure.Services;
using Tabatine.Worker.IntegrationTests.Helpers;
using WireMock.ResponseBuilders;
using WireMock.RequestBuilders;
using Xunit;
using System.Net;

namespace Tabatine.Worker.IntegrationTests.Services;

[Trait("Category", "Integrated")]
public class OmieResilienciaIntegrationTests(HttpMockIntegrationTestWebAppFactory factory) : BaseHttpMockIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Tentar_Novamente_Quando_Receber_Erro_425_Da_Omie()
    {
        // Arrange
        // Configura o WireMock para falhar na primeira vez com 425 e ter sucesso na segunda usando Scenarios
        Factory.OmieMockServer
            .Given(Request.Create().WithPath("/geral/clientes/").UsingPost())
            .InScenario("Resilience")
            .WillSetStateTo("Retry1")
            .RespondWith(Response.Create()
                .WithStatusCode(425)
                .WithBody("{\"faultstring\": \"Too Many Requests\"}"));

        Factory.OmieMockServer
            .Given(Request.Create().WithPath("/geral/clientes/").UsingPost())
            .InScenario("Resilience")
            .WhenStateIs("Retry1")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"pagina\": 1, \"total_de_paginas\": 1, \"registros\": 0, \"clientes_cadastro\": []}"));

        using var scope = Factory.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ClienteSyncService>();

        // Act
        // A política de retry deve entrar em ação e o método deve completar com sucesso na segunda tentativa
        await syncService.SyncAllAsync(CancellationToken.None);

        // Assert
        // Verificamos se houve 2 chamadas no log do WireMock
        Factory.OmieMockServer.LogEntries.Should().HaveCount(2, "O cliente deve ter tentado novamente após o erro 425.");
    }
}
