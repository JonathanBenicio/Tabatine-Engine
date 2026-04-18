using Tabatine.Core.Entities;
using Tabatine.Omie.Client.Models.Financeiro;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Infrastructure.Data;
using System.Net.Http.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Tabatine.Worker.IntegrationTests;

public class FinanceiroWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_ContaReceber_E_Persistir_No_Banco_Com_Sucesso()
    {
        // Arrange
        var omieId = 998877L;
        var titulo = new OmieContaReceber
        {
            CodigoLancamentoOmie = omieId,
            NumeroDocumento = "DOC-WEBHOOK",
            ValorDocumento = 1500.50m,
            DataEmissao = "17/04/2026", // OBRIGATÓRIO para ParseData
            DataVencimento = "20/04/2026", // OBRIGATÓRIO para ParseData
            StatusTitulo = "RECEBER"
        };

        Factory.OmieClientMock.ConsultarContaReceberAsync(omieId, Arg.Any<CancellationToken>())
            .Returns(titulo);

        var webhookEvent = new
        {
            topic = "Financas.ContaReceber.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                nCodLanc = omieId
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        await ProcessWebhooksAsync();

        // Assert
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var registroDB = await dbContext.TitulosReceber.AsNoTracking().FirstOrDefaultAsync(c => c.OmieId == omieId);

        Assert.NotNull(registroDB);
        Assert.Equal(1500.50m, registroDB!.ValorDocumento);
        Assert.Equal("RECEBER", registroDB.StatusTitulo);
    }

    [Fact]
    public async Task Deve_Processar_Webhook_ContaPagar_Excluido_E_Marcar_Como_Cancelado()
    {
        // Arrange
        var omieId = 112233L;
        
        // Seed: Criar conta a pagar no banco antes da exclusão
        using (var scopeInit = Factory.Services.CreateScope())
        {
            var dbContextInit = scopeInit.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContextInit.TitulosPagar.Add(new TituloPagar 
            { 
                Id = Guid.NewGuid(), 
                OmieId = omieId, 
                NumeroDocumento = "PAG-99", 
                StatusTitulo = "APAGAR",
                ValorDocumento = 100,
                DataEmissao = DateTime.UtcNow,
                DataVencimento = DateTime.UtcNow.AddDays(10)
            });
            await dbContextInit.SaveChangesAsync();
        }

        var webhookEvent = new
        {
            topic = "Financas.ContaPagar.Excluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                nCodLanc = omieId
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        await ProcessWebhooksAsync();

        // Assert
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var registroDB = await dbContext.TitulosPagar.AsNoTracking().FirstOrDefaultAsync(c => c.OmieId == omieId);

        Assert.NotNull(registroDB);
        Assert.Equal("CANCELADO", registroDB!.StatusTitulo);
    }

    [Fact]
    public async Task Deve_Ignorar_Webhook_ContaReceber_Se_API_Falhar_Com_Retry_Fila()
    {
        // Arrange
        var omieId = 445566L;

        // Simular falha na API Omie (Lança erro)
        Factory.OmieClientMock.ConsultarContaReceberAsync(omieId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("API Offline"));

        var webhookEvent = new
        {
            topic = "Financas.ContaReceber.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new { nCodLanc = omieId }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        // Tenta processar
        await ProcessWebhooksAsync();

        // Assert
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var registroDB = await dbContext.TitulosReceber.AsNoTracking().FirstOrDefaultAsync(c => c.OmieId == omieId);

        // Não deve ter sido inserido devido ao erro da API Omie
        Assert.Null(registroDB);
    }
}
