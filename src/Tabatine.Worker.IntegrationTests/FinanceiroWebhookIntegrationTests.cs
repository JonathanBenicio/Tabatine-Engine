using System.Net.Http.Json;
using Tabatine.Core.Entities;
using Tabatine.Omie.Client.Models.Financeiro;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Infrastructure.Data;
using Xunit;
using NSubstitute;

namespace Tabatine.Worker.IntegrationTests;

public class FinanceiroWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_ContaReceber_E_Persistir_No_Banco_Com_Sucesso()
    {
        // Arrange
        var omieId = 987654321L;
        var omieReceber = new OmieContaReceber
        {
            CodigoLancamentoOmie = omieId,
            CodigoClienteFornecedor = 123456L,
            NumeroDocumento = "DOC-REC-001",
            DataEmissao = "10/04/2026",
            DataVencimento = "20/04/2026",
            ValorDocumento = 1500.50m,
            StatusTitulo = "ABERTO",
            Info = new OmieContaReceberInfo { DAlt = "10/04/2026", HAlt = "15:30:00" }
        };

        Factory.OmieClientMock.ConsultarContaReceberAsync(omieId, Arg.Any<CancellationToken>())
            .Returns(omieReceber);

        var webhookEvent = new
        {
            topic = "Financas.ContaReceber.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                nCodLanc = omieId,
                codigo_lancamento_omie = omieId,
                status_titulo = "ABERTO"
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        TituloReceber? registroDB = null;
        var timeout = TimeSpan.FromSeconds(20);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            registroDB = await dbContext.TitulosReceber
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.OmieId == omieId);
            
            if (registroDB != null) break;
            await Task.Delay(500);
        }

        Assert.NotNull(registroDB);
        Assert.Equal("DOC-REC-001", registroDB!.NumeroDocumento);
        Assert.Equal(1500.50m, registroDB.ValorDocumento);
        Assert.Equal("ABERTO", registroDB.StatusTitulo);
    }

    [Fact]
    public async Task Deve_Processar_Webhook_ContaPagar_Excluido_E_Marcar_Como_Cancelado()
    {
        // Arrange
        var omieId = 11223344L;
        
        // Primeiro criamos o registro via mock do Sync normal ou seed direto no banco
        // Vamos seedar direto para garantir que o CancelById encontre o registro
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tituloPagar = new TituloPagar
            {
                Id = Guid.NewGuid(),
                OmieId = omieId,
                NumeroDocumento = "DOC-PAG-DEL",
                StatusTitulo = "ABERTO",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.TitulosPagar.Add(tituloPagar);
            await dbContext.SaveChangesAsync();
        }

        var webhookEvent = new
        {
            topic = "Financas.ContaPagar.Excluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                nCodLanc = omieId,
                codigo_lancamento_omie = omieId
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        TituloPagar? registroDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            registroDB = await dbContext.TitulosPagar.FirstOrDefaultAsync(c => c.OmieId == omieId);
            
            if (registroDB != null && registroDB.StatusTitulo == "CANCELADO") break;
            await Task.Delay(500);
        }

        Assert.NotNull(registroDB);
        Assert.Equal("CANCELADO", registroDB!.StatusTitulo);
    }

    [Fact]
    public async Task Teste_De_Resiliencia_Comportamento_API_Financas_Lenta_Indisponivel()
    {
        // Arrange
        var omieId = 333444555L;

        // Simulando que a API Omie cravou (ex: Rate limit 429 ou Timeout 500)
        // Simulando que a API Omie cravou (ex: Rate limit 429 ou Timeout 500)
        Factory.OmieClientMock.ConsultarContaReceberAsync(omieId, Arg.Any<CancellationToken>())
            .Returns(x => Task.FromException<Tabatine.Omie.Client.Models.Financeiro.OmieContaReceber?>(new System.Net.Http.HttpRequestException("Rate Limit Exceeded - 429")));

        var webhookEvent = new
        {
            topic = "Financas.ContaReceber.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                nCodLanc = omieId,
                codigo_lancamento_omie = omieId
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        
        // Assert
        // O Webhook Receiver deve aceitar (200 OK ou 202 Accepted) para não bloquear o ERP emissor,
        // mas colocar no channel de background (DLQ handler).
        response.EnsureSuccessStatusCode();

        TituloReceber? registroDB = null;
        var timeout = TimeSpan.FromSeconds(5);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            registroDB = await dbContext.TitulosReceber.FirstOrDefaultAsync(c => c.OmieId == omieId);
            
            if (registroDB != null) break;
            await Task.Delay(500);
        }

        // Não deve ter sido inserido devido ao erro da API Omie
        Assert.Null(registroDB);

        // Opcional: Aqui poderíamos validar a tabela WebhookEvents onde DLQ = true caso esteja mapeado
        // usando a DbContext local.
    }
}
