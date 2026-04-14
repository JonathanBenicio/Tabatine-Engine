using Tabatine.Omie.Client.Models.Bancos;
using Tabatine.Omie.Client.Models.FormaPagamento;
using Tabatine.Omie.Client.Models.Geral;

namespace Tabatine.Worker.IntegrationTests;

/// <summary>
/// Testes de integração para entidades auxiliares de catálogo:
/// Banco, FormaPagamento, CondicaoPagamento e MeioPagamento.
/// Sprint 2 — Issue #36 (filho de #25)
/// </summary>
public class CatalogoAuxiliarIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Sincronizar_Banco_Via_SyncAll()
    {
        // Arrange — Mock retorna um banco
        Factory.OmieClientMock.ListarBancosAsync(1, Arg.Any<CancellationToken>())
            .Returns(new ListarBancosResponse
            {
                Pagina = 1,
                TotalDePaginas = 1,
                Bancos = new List<OmieBanco>
                {
                    new() { Codigo = "341", Nome = "Itaú Unibanco", CodigoIspb = "60701190", Tipo = "B" }
                }
            });

        // Act — Sync direto via serviço
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.BancoSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var banco = await dbContext.Bancos.FirstOrDefaultAsync(b => b.CodigoBanco == "341");

            Assert.NotNull(banco);
            Assert.Equal("Itaú Unibanco", banco!.Nome);
            Assert.Equal("341", banco.CodigoBanco);
        }
    }

    [Fact]
    public async Task Deve_Sincronizar_FormaPagamento_Via_SyncAll()
    {
        // Arrange
        Factory.OmieClientMock.ListarFormasPagVendasAsync(1, Arg.Any<CancellationToken>())
            .Returns(new ListarFormasPagVendasResponse
            {
                Pagina = 1,
                TotalDePaginas = 1,
                FormasPagamento = new List<OmieFormaPagamento>
                {
                    new() { Codigo = "PIX", Descricao = "Pix" },
                    new() { Codigo = "CC", Descricao = "Cartão de Crédito" },
                    new() { Codigo = "CD", Descricao = "Cartão de Débito" }
                }
            });

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.FormaPagamentoSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var pix = await dbContext.FormasPagamento.FirstOrDefaultAsync(f => f.Codigo == "PIX");
            var cartaoCredito = await dbContext.FormasPagamento.FirstOrDefaultAsync(f => f.Codigo == "CC");

            Assert.NotNull(pix);
            Assert.Equal("Pix", pix!.Descricao);
            Assert.NotNull(cartaoCredito);
            Assert.Equal("Cartão de Crédito", cartaoCredito!.Descricao);
        }
    }

    [Fact]
    public async Task Deve_Sincronizar_CondicaoPagamento_Via_SyncAll()
    {
        // Arrange
        Factory.OmieClientMock.ListarParcelasAsync(1, Arg.Any<CancellationToken>())
            .Returns(new ListarParcelasResponse
            {
                Pagina = 1,
                TotalDePaginas = 1,
                Cadastros = new List<ParcelaOmie>
                {
                    new() { Codigo = 201, Descricao = "À Vista", QuantidadeParcelas = 1 },
                    new() { Codigo = 202, Descricao = "30/60 dias", QuantidadeParcelas = 2 }
                }
            });

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.CondicaoPagamentoSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var aVista = await dbContext.CondicoesPagamento.FirstOrDefaultAsync(c => c.Codigo == "201");
            var prazo = await dbContext.CondicoesPagamento.FirstOrDefaultAsync(c => c.Codigo == "202");

            Assert.NotNull(aVista);
            Assert.Equal("À Vista", aVista!.Descricao);
            Assert.NotNull(prazo);
            Assert.Equal("30/60 dias", prazo!.Descricao);
        }
    }

    [Fact]
    public async Task Deve_Sincronizar_MeioPagamento_Via_SyncAll()
    {
        // Arrange
        Factory.OmieClientMock.ListarMeiosPagamentoAsync(Arg.Any<CancellationToken>())
            .Returns(new MeiosPagamentoPesquisarResponse
            {
                MeiosPagamentoLista = new List<OmieMeioPagamento>
                {
                    new() { Codigo = "BOLETO", Descricao = "Boleto Bancário" },
                    new() { Codigo = "TRANSF", Descricao = "Transferência Bancária" }
                }
            });

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.MeioPagamentoSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var boleto = await dbContext.MeiosPagamento.FirstOrDefaultAsync(m => m.Codigo == "BOLETO");
            var transf = await dbContext.MeiosPagamento.FirstOrDefaultAsync(m => m.Codigo == "TRANSF");

            Assert.NotNull(boleto);
            Assert.Equal("Boleto Bancário", boleto!.Descricao);
            Assert.NotNull(transf);
            Assert.Equal("Transferência Bancária", transf!.Descricao);
        }
    }

    [Fact]
    public async Task Deve_Sincronizar_Banco_Duas_Vezes_Sem_Duplicar_Idempotencia()
    {
        // Valida que o sync é idempotente: rodar duas vezes não duplica registros
        var resposta = new ListarBancosResponse
        {
            Pagina = 1,
            TotalDePaginas = 1,
            Bancos = new List<OmieBanco>
            {
                new() { Codigo = "999", Nome = "Banco Idempotencia Teste", CodigoIspb = "00000999", Tipo = "B" }
            }
        };

        Factory.OmieClientMock.ListarBancosAsync(1, Arg.Any<CancellationToken>())
            .Returns(resposta);

        // Sync 2x
        for (var i = 0; i < 2; i++)
        {
            using var scope = Factory.Services.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.BancoSyncService>();
            await syncService.SyncAllAsync();
        }

        // Assert — deve existir exatamente 1 registro com Codigo = "999"
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var count = await dbContext.Bancos.CountAsync(b => b.CodigoBanco == "999");
            Assert.Equal(1, count);
        }
    }
}
