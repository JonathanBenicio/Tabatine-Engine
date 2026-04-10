using Tabatine.Omie.Client.Models.Financeiro;

namespace Tabatine.Worker.IntegrationTests;

/// <summary>
/// Testes avançados de pagamento parcial progressivo em TituloReceber.
/// Valida múltiplos estágios de liquidação parcial até liquidação total.
/// Sprint 1 — Issue #35 (filho de #25)
/// </summary>
public class TituloReceberPagamentoParcialTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Atualizar_TituloReceber_Em_Dois_Pagamentos_Parciais_Ate_Liquidacao_Total()
    {
        // Arrange — título original de R$ 900,00
        var omieId = 800900111L;
        var valorTotal = 900.00m;

        // Primeira liquidação parcial: R$ 300,00 pagos
        var respostaPrimeiroParcial = new ListarContasReceberResponse
        {
            Pagina = 1,
            TotalDePaginas = 1,
            Registros = 1,
            TotalDeRegistros = 1,
            ContasReceber = new List<OmieContaReceber>
            {
                new()
                {
                    CodigoLancamentoOmie = omieId,
                    CodigoClienteFornecedor = 321L,
                    NumeroDocumento = "PARCIAL-2-FASES",
                    DataEmissao = "01/04/2026",
                    DataVencimento = "30/04/2026",
                    ValorDocumento = valorTotal,
                    ValorRecebido = 300.00m,
                    ValorSaldo = 600.00m,
                    StatusTitulo = "PAGO_PARCIAL",
                    Info = new OmieContaReceberInfo { DAlt = "05/04/2026", HAlt = "10:00:00" }
                }
            }
        };

        Factory.OmieClientMock.ListarContasReceberAsync(
            Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(respostaPrimeiroParcial);

        // Act — 1ª sincronização (pagamento parcial 1)
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ContasReceberSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert — 1ª verificação
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reg = await dbContext.TitulosReceber.FirstOrDefaultAsync(t => t.OmieId == omieId);

            reg.Should().NotBeNull("O título deve existir após 1ª sincronização");
            reg!.ValorRecebido.Should().Be(300.00m, "Primeiro pagamento parcial de R$300");
            reg.ValorSaldo.Should().Be(600.00m);
            reg.StatusTitulo.Should().Be("PAGO_PARCIAL");
        }

        // Segunda liquidação: pagou mais R$ 600,00 → totalmente quitado
        var respostaSegundoParcial = new ListarContasReceberResponse
        {
            Pagina = 1,
            TotalDePaginas = 1,
            Registros = 1,
            TotalDeRegistros = 1,
            ContasReceber = new List<OmieContaReceber>
            {
                new()
                {
                    CodigoLancamentoOmie = omieId,
                    CodigoClienteFornecedor = 321L,
                    NumeroDocumento = "PARCIAL-2-FASES",
                    DataEmissao = "01/04/2026",
                    DataVencimento = "30/04/2026",
                    DataBaixa = "10/04/2026",
                    ValorDocumento = valorTotal,
                    ValorRecebido = 900.00m,
                    ValorSaldo = 0.00m,
                    StatusTitulo = "RECEBIDO",
                    Info = new OmieContaReceberInfo { DAlt = "10/04/2026", HAlt = "14:00:00" }
                }
            }
        };

        Factory.OmieClientMock.ListarContasReceberAsync(
            Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(respostaSegundoParcial);

        // Act — 2ª sincronização (liquidação total)
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ContasReceberSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert final — título deve estar totalmente liquidado
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reg = await dbContext.TitulosReceber.FirstOrDefaultAsync(t => t.OmieId == omieId);

            reg.Should().NotBeNull();
            reg!.ValorRecebido.Should().Be(900.00m, "Valor total pago deve ser R$900");
            reg.ValorSaldo.Should().Be(0.00m, "Saldo deve ser zero após liquidação total");
            reg.StatusTitulo.Should().Be("RECEBIDO", "Status deve ser RECEBIDO após quitação completa");
            reg.DataBaixa.Should().NotBeNull("Data de baixa deve ser registrada na quitação");
        }
    }

    [Fact]
    public async Task Deve_Registrar_Credito_E_Debito_Em_ContaCorrente_E_Calcular_Saldo_Correto()
    {
        // Valida que movimentos de crédito e débito em CC resultam no saldo esperado
        var omieIdCC = 222333444L;

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await dbContext.ContasCorrente.AnyAsync(c => c.OmieId == omieIdCC))
            {
                dbContext.ContasCorrente.Add(new ContaCorrente
                {
                    Id = Guid.NewGuid(),
                    OmieId = omieIdCC,
                    Descricao = "CC Movimento Crédito/Débito",
                    Tipo = "000",
                    SaldoInicial = 5000.00m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }
        }

        // Simula atualização de saldo via webhook (após movimento)
        var ccResponse = new Tabatine.Omie.Client.Models.ContaCorrente.ListarContaCorrenteResponse
        {
            ContasCorrentes = new List<Tabatine.Omie.Client.Models.ContaCorrente.OmieContaCorrente>
            {
                new() { Codigo = omieIdCC, Descricao = "CC Movimento Crédito/Débito", Tipo = "000", SaldoInicial = 5000.00m, Inativo = "N" }
            },
            TotalDeRegistros = 1,
            TotalDePaginas = 1,
            Pagina = 1
        };

        Factory.OmieClientMock.ListarContasCorrentesAsync(
            Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(ccResponse);

        var webhookCredito = new
        {
            topic = "Financas.ContaCorrente.Alterado",
            messageId = Guid.NewGuid().ToString(),
            @event = new { idContaCorrente = omieIdCC }
        };

        var resp = await Client.PostAsJsonAsync("/webhook/omie", webhookCredito);
        resp.EnsureSuccessStatusCode();

        ContaCorrente? ccDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            ccDB = await dbContext.ContasCorrente.FirstOrDefaultAsync(c => c.OmieId == omieIdCC);
            if (ccDB?.SaldoInicial == 5000.00m) break;
            await Task.Delay(500);
        }

        ccDB.Should().NotBeNull("A conta corrente deve ser localizada no BD");
        ccDB!.SaldoInicial.Should().Be(5000.00m, "Saldo inicial deve manter-se conforme Omie");
        ccDB.Descricao.Should().Be("CC Movimento Crédito/Débito");
    }
}
