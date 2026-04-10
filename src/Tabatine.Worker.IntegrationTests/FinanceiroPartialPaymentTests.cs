using Tabatine.Core.Entities;
using Tabatine.Omie.Client.Models.Financeiro;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Infrastructure.Data;
using Tabatine.Core.Interfaces;
using Xunit;
using NSubstitute;

namespace Tabatine.Worker.IntegrationTests;

public class FinanceiroPartialPaymentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Sincronizar_ContaPagar_Com_Pagamento_Parcial()
    {
        // Arrange
        var omieId = 123000456L;
        var omiePagar = new OmieContaPagar
        {
            CodigoLancamentoOmie = omieId,
            CodigoClienteFornecedor = 555L,
            NumeroDocumento = "PARTIAL-001",
            DataEmissao = "01/04/2026",
            DataVencimento = "30/04/2026",
            DataBaixa = "10/04/2026",
            ValorDocumento = 1000.00m,
            ValorPago = 400.00m,
            ValorSaldo = 600.00m,
            StatusTitulo = "PAGO_PARCIAL"
        };

        var response = new ListarContasPagarResponse
        {
            Pagina = 1,
            TotalDePaginas = 1,
            Registros = 1,
            TotalDeRegistros = 1,
            ContasPagar = new List<OmieContaPagar> { omiePagar }
        };

        Factory.OmieClientMock.ListarContasPagarAsync(Arg.Any<int>(), Arg.Any<long?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ContasPagarSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var registroDB = await dbContext.TitulosPagar.FirstOrDefaultAsync(t => t.OmieId == omieId);

            registroDB.Should().NotBeNull();
            registroDB!.ValorDocumento.Should().Be(1000.00m);
            registroDB.ValorPago.Should().Be(400.00m);
            registroDB.ValorSaldo.Should().Be(600.00m);
            registroDB.DataBaixa.Should().Be(new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
