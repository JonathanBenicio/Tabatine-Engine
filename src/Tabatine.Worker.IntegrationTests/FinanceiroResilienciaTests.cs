using Tabatine.Omie.Client.Models.Financeiro;

namespace Tabatine.Worker.IntegrationTests;

/// <summary>
/// Testes de resiliência e tratamento de falhas para o módulo Financeiro.
/// Épico #27: Auditoria de Testes — Financeiro.
/// </summary>
public class FinanceiroResilienciaTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Completar_Sincronizacao_Mesmo_Com_API_Lenta()
    {
        // Arrange
        var omieId = 777777L;
        var response = new ListarContasReceberResponse
        {
            Pagina = 1,
            TotalDeRegistros = 1,
            TotalDePaginas = 1,
            ContasReceber = new List<OmieContaReceber> 
            { 
                new() 
                { 
                    CodigoLancamentoOmie = omieId, 
                    NumeroDocumento = "LENTA001", 
                    ValorDocumento = 100,
                    DataEmissao = "10/04/2026",
                    DataVencimento = "15/04/2026"
                } 
            }
        };

        // Simula latência de 2 segundos na API
        Factory.OmieClientMock.ListarContasReceberAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                await Task.Delay(2000, (CancellationToken)x[4]);
                return response;
            });

        // Act
        DateTime start = DateTime.UtcNow;
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ContasReceberSyncService>();
            await syncService.SyncAllAsync();
        }
        DateTime end = DateTime.UtcNow;

        // Assert
        Assert.True((end - start).TotalMilliseconds > 2000);

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var titulo = await dbContext.TitulosReceber.FirstOrDefaultAsync(t => t.OmieId == omieId);
            Assert.NotNull(titulo);
        }
    }

    [Fact]
    public async Task Deve_Falhar_Com_Timeout_Se_API_Exceder_Limite_Critico()
    {
        // Arrange
        // Simula uma API que demora demais (ex: 30s)
        Factory.OmieClientMock.ListarContasReceberAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                await Task.Delay(30000, (CancellationToken)x[4]);
                return new ListarContasReceberResponse();
            });

        // Act & Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ContasReceberSyncService>();
            
            // O SyncService deve propagar o CancellationToken que expira em 2s
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            
            await Assert.ThrowsAsync<OperationCanceledException>(() => syncService.SyncAllAsync(cts.Token));
        }
    }
}
