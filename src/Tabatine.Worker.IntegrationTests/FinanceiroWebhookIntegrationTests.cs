using System.Net.Http.Json;
using Tabatine.Core.Entities;
using Tabatine.Omie.Client.Models.Financeiro;
using FluentAssertions;
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
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            registroDB = await dbContext.TitulosReceber.FirstOrDefaultAsync(c => c.OmieId == omieId);
            
            if (registroDB != null) break;
            await Task.Delay(500);
        }

        registroDB.Should().NotBeNull("A conta a receber deve ser persistida");
        registroDB!.NumeroDocumento.Should().Be("DOC-REC-001");
        registroDB.ValorDocumento.Should().Be(1500.50m);
        registroDB.StatusTitulo.Should().Be("ABERTO");
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

        registroDB.Should().NotBeNull();
        registroDB!.StatusTitulo.Should().Be("CANCELADO", "O título deve ser marcado como CANCELADO ao receber o evento de exclusão");
    }
}
