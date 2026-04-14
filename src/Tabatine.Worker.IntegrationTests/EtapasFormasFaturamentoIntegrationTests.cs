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

        // Act - Chamar o serviço diretamente em vez de passar pelo SyncManager (que tem delays artificiais)
        using var scope = Factory.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.EtapaFaturamentoSyncService>();
        await syncService.SyncAllAsync();

        // Assert
        using var readScope = Factory.Services.CreateScope();
        var dbContext = readScope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Data.AppDbContext>();
        
        var etapaDB = await dbContext.EtapasFaturamento
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Codigo == codigoEtapa);

        etapaDB.Should().NotBeNull("A etapa de faturamento deve ser persistida.");
        etapaDB!.Descricao.Should().Be(descricaoEtapa);
    }
}
