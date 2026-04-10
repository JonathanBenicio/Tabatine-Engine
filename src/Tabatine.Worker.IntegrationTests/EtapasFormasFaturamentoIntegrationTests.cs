using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net.Http.Json;
using Tabatine.Core.Entities;
using Tabatine.Omie.Client.Models.EtapaFaturamento;
using Tabatine.Omie.Client.Models.FormaPagamento;
using Xunit;

namespace Tabatine.Worker.IntegrationTests;

public class EtapasFormasFaturamentoIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Replicacao_EtapaFaturamento_Com_Sucesso()
    {
        // Arrange
        var codigoEtapa = "50";
        var descricaoEtapa = "Separacao Estoque Teste";

        // Mock
        var responseOmie = new ListarEtapasFaturamentoResponse
        {
            Pagina = 1,
            TotalDePaginas = 1,
            Cadastros = new List<OmieOperacaoEtapa>
            {
                new()
                {
                    CodigoOperacao = "10",
                    DescricaoOperacao = "Venda",
                    Etapas = new List<OmieEtapaFaturamento>
                    {
                        new() { Codigo = codigoEtapa, Descricao = descricaoEtapa }
                    }
                }
            }
        };

        Factory.OmieClientMock.ListarEtapasFaturamentoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(responseOmie);

        // Nao tem webhook especifico para etapa, vamos assumir que o webhook de Pedido ou Sincronismo Manual invoca
        // Aqui simulo o sync direto para as Etapas de Faturamento para o teste.
        using var scope = Factory.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Core.Interfaces.ISyncService>();

        // Act
        // Simplificação: Roda o Sync All ou apenas invoco a listagem/mapeamento via repository? 
        // Vamos forçar o db update se fosse recebido no worker:
        var webhookEvent = new
        {
            topic = "System.ManualSync",
            messageId = Guid.NewGuid().ToString(),
            @event = new { ModuleRequest = "EtapasFaturamento" }
        };

        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        EtapaFaturamento? etapaDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var readScope = Factory.Services.CreateScope();
            var dbContext = readScope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Data.AppDbContext>();
            etapaDB = await dbContext.EtapasFaturamento.FirstOrDefaultAsync(e => e.Codigo == codigoEtapa);
            
            if (etapaDB != null) break;
            await Task.Delay(500);
        }

        etapaDB.Should().NotBeNull("A etapa de faturamento deve ser persistida.");
        etapaDB!.Descricao.Should().Be(descricaoEtapa);
    }
}
