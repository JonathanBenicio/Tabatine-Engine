using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Diagnostics;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Tabatine.Omie.Client.Models.Bancos;
using Tabatine.Omie.Client.Models.Clientes;
using Tabatine.Omie.Client.Models.ContaCorrente;
using Tabatine.Omie.Client.Models.Estoque;
using Tabatine.Omie.Client.Models.EtapaFaturamento;
using Tabatine.Omie.Client.Models.Financeiro;
using Tabatine.Omie.Client.Models.FormaPagamento;
using Tabatine.Omie.Client.Models.Geral;
using Tabatine.Omie.Client.Models.NotasFiscais;
using Tabatine.Omie.Client.Models.Pedidos;
using Tabatine.Omie.Client.Models.Produtos;
using Tabatine.Omie.Client.Models.Vendedores;

namespace Tabatine.Worker.IntegrationTests.Services;

public class AllSyncsIntegrationTests : BaseIntegrationTest
{
    public AllSyncsIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Deve_Sincronizar_Todas_As_Entidades_Com_Sucesso()
    {
        // 1. Arrange - Setup Mocks for all 14 entities
        SetupAllMocks();

        using var scope = Factory.Services.CreateScope();
        var syncManager = scope.ServiceProvider.GetRequiredService<ISyncService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Limpar estados de sync para garantir execução total
        await db.Database.ExecuteSqlRawAsync("DELETE FROM integration_sync_states;");

        // 2. Act
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await syncManager.SyncAllAsync(cts.Token);

        // 3. Assert - Check IntegrationSyncStates
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var syncStates = await assertDb.IntegrationSyncStates.ToListAsync();
        
        foreach (var s in syncStates) 
        {
            Console.WriteLine($"[TEST DEBUG] Sincronizado: {s.ModuleName} em {s.LastSyncDate}");
        }

        if (syncStates.Count != 14)
        {
            var expectedKeys = new[] { "Bancos", "MeiosPagamento", "EtapasFaturamento", "FormasPagamento", "CondicoesPagamento", "Vendedores", "ContasCorrente", "Clientes", "Produtos", "Estoque", "Pedidos", "NotasFiscais", "ContasReceber", "ContasPagar" };
            var missing = expectedKeys.Except(syncStates.Select(s => s.ModuleName)).ToList();
            Console.WriteLine($"[TEST DEBUG] FALTANDO: {string.Join(", ", missing)}");
        }

        Assert.Equal(14, syncStates.Count);

        // Assert - Check Data Persistence in tables (one by one)
        Assert.True(await assertDb.Bancos.AnyAsync(b => b.CodigoBanco == "001"), "Bancos");
        Assert.True(await assertDb.MeiosPagamento.AnyAsync(m => m.Codigo == "BOLETO"), "Meios de Pagamento");
        Assert.True(await assertDb.EtapasFaturamento.AnyAsync(e => e.Codigo == "10"), "Etapas de Faturamento");
        Assert.True(await assertDb.FormasPagamento.AnyAsync(f => f.Codigo == "BOL"), "Formas de Pagamento");
        Assert.True(await assertDb.CondicoesPagamento.AnyAsync(c => c.Codigo == "101"), "Condições de Pagamento");
        Assert.True(await assertDb.Vendedores.AnyAsync(v => v.OmieId == 555), "Vendedores");
        Assert.True(await assertDb.ContasCorrente.AnyAsync(c => c.OmieId == 444), "Contas Corrente");
        Assert.True(await assertDb.Clientes.AnyAsync(c => c.OmieId == 111), "Clientes");
        Assert.True(await assertDb.Produtos.AnyAsync(p => p.OmieId == 222), "Produtos");
        Assert.True(await assertDb.LocaisEstoque.AnyAsync(l => l.OmieId == 888), "Locais de Estoque");
        Assert.True(await assertDb.ProdutosEstoque.AnyAsync(), "Saldos de Estoque");
        Assert.True(await assertDb.PedidosVenda.AnyAsync(p => p.OmieId == 333), "Pedidos de Venda");
        Assert.True(await assertDb.NotasFiscais.AnyAsync(n => n.OmieId == 777), "Notas Fiscais");
        Assert.True(await assertDb.TitulosReceber.AnyAsync(t => t.OmieId == 666), "Contas a Receber");
        Assert.True(await assertDb.TitulosPagar.AnyAsync(t => t.OmieId == 999), "Contas a Pagar");
    }

    private void SetupAllMocks()
    {
        var mock = Factory.OmieClientMock;

        // 1. Bancos
        mock.ListarBancosAsync(1, Arg.Any<CancellationToken>()).Returns(new ListarBancosResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            Bancos = new List<OmieBanco> { new() { Codigo = "001", Nome = "Banco do Brasil", CodigoIspb = "00000000", Tipo = "B" } }
        });

        // 2. Meios de Pagamento
        mock.ListarMeiosPagamentoAsync(Arg.Any<CancellationToken>()).Returns(new MeiosPagamentoPesquisarResponse
        {
            MeiosPagamentoLista = new List<OmieMeioPagamento> { new() { Codigo = "BOLETO", Descricao = "Boleto Bancário" } }
        });

        // 3. Etapas de Faturamento
        mock.ListarEtapasFaturamentoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new ListarEtapasFaturamentoResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            Cadastros = new List<OmieOperacaoEtapa> 
            { 
                new() 
                { 
                    CodigoOperacao = "10", 
                    DescricaoOperacao = "Faturamento", 
                    Etapas = new List<OmieEtapaFaturamento> { new() { Codigo = "10", Descricao = "Faturamento" } } 
                } 
            }
        });

        // 4. Formas de Pagamento
        mock.ListarFormasPagVendasAsync(1, Arg.Any<CancellationToken>()).Returns(new ListarFormasPagVendasResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            FormasPagamento = new List<OmieFormaPagamento> { new() { Codigo = "BOL", Descricao = "Boleto" } }
        });

        // 5. Condições de Pagamento (ListarParcelasAsync)
        mock.ListarParcelasAsync(1, Arg.Any<CancellationToken>()).Returns(new ListarParcelasResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            Cadastros = new List<ParcelaOmie> { new() { Codigo = 101, Descricao = "30 dias", QuantidadeParcelas = 1 } }
        });

        // 6. Vendedores
        mock.ListarVendedoresAsync(1, null, null, Arg.Any<CancellationToken>()).Returns(new ListarVendedoresResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            Vendedores = new List<OmieVendedor> { new() { Codigo = 555, Nome = "Vendedor Teste" } }
        });

        // 7. Contas Correntes
        mock.ListarContasCorrentesAsync(1, null, null, Arg.Any<CancellationToken>()).Returns(new ListarContaCorrenteResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            ContasCorrentes = new List<OmieContaCorrente> { new() { Codigo = 444, Descricao = "Conta Principal" } }
        });

        // 8. Clientes
        mock.ListarClientesAsync(1, Arg.Any<DateTime?>(), null, Arg.Any<CancellationToken>()).Returns(new ListarClientesResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            ClientesCadastro = new List<OmieCliente> { new() { CodigoClienteOmie = 111, RazaoSocial = "Cliente Teste", CnpjCpf = "00000000000191" } }
        });

        // 9. Produtos
        mock.ListarProdutosAsync(1, Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(new ListarProdutosResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            ProdutosCadastro = new List<OmieProduto> { new() { CodigoProduto = 222, Descricao = "Produto Teste", Codigo = "PRD001" } }
        });

        // 10. Estoque - Locais
        mock.ListarLocaisEstoqueAsync(Arg.Any<CancellationToken>()).Returns(new List<LocalEstoqueDto> 
        { 
            new() { CodigoLocalEstoque = 888, Descricao = "Almoxarifado" } 
        }.ToAsyncEnumerable());

        // 10. Estoque - Saldos
        mock.StreamPosicaoEstoqueAsync(Arg.Any<ListarPosEstoqueRequest>(), Arg.Any<CancellationToken>()).Returns(new List<ProdutoEstoqueDto> 
        { 
            new() { CodProd = 222, CodigoLocalEstoque = 888, Saldo = 100 } 
        }.ToAsyncEnumerable());

        // 11. Pedidos (Requires a customer to exist)
        mock.ListarPedidosAsync(1, Arg.Any<DateTime?>(), null, Arg.Any<CancellationToken>()).Returns(new ListarPedidosResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            PedidosVenda = new List<OmiePedido> 
            { 
                new() 
                { 
                    Cabecalho = new() { CodigoPedido = 333, NumeroPedido = "PED001", CodigoCliente = 111, Etapa = "10" },
                    TotalPedido = new() { ValorTotalPedido = 100 },
                    Det = new() 
                    { 
                        new() { Ide = new() { CodigoItem = 1 }, Produto = new() { CodigoProduto = 222, Quantidade = 1, ValorUnitario = 100, ValorTotal = 100 } } 
                    }
                } 
            }
        });

        // 12. Notas Fiscais
        mock.ListarNotasFiscaisAsync(1, Arg.Any<DateTime?>(), null, Arg.Any<CancellationToken>()).Returns(new ListarNotasFiscaisResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            NotasFiscais = new List<OmieNotaFiscal> 
            { 
                new() 
                { 
                    Ide = new() { Numero = "001", DataEmissao = "01/01/2024" },
                    Compl = new() { IdNf = 777, IdPedido = 333 },
                    Destinatario = new() { CodigoCliente = 111 },
                    Info = new() { DAlt = "01/01/2024", HAlt = "10:00:00" },
                    Total = new() { IcmsTot = new() { ValorNota = 100 } }
                } 
            }
        });

        // 13. Contas a Receber
        mock.ListarContasReceberAsync(1, Arg.Any<DateTime?>(), null, null, Arg.Any<CancellationToken>()).Returns(new ListarContasReceberResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            ContasReceber = new List<OmieContaReceber> 
            { 
                new() { CodigoLancamentoOmie = 666, NumeroDocumento = "REC001", CodigoClienteFornecedor = 111, ValorDocumento = 100, DataVencimento = "31/12/2024", DataEmissao = "01/01/2024", StatusTitulo = "ABERTO" } 
            }
        });

        // 14. Contas a Pagar
        mock.ListarContasPagarAsync(1, null, Arg.Any<DateTime?>(), null, null, Arg.Any<CancellationToken>()).Returns(new ListarContasPagarResponse
        {
            Pagina = 1, TotalDePaginas = 1,
            ContasPagar = new List<OmieContaPagar> 
            { 
                new() { CodigoLancamentoOmie = 999, NumeroDocumento = "PAG001", CodigoClienteFornecedor = 111, ValorDocumento = 50, DataVencimento = "31/12/2024", StatusTitulo = "ABERTO" } 
            }
        });
    }
}
