using Tabatine.Omie.Client.Models.Pedidos;

namespace Tabatine.Worker.IntegrationTests;

/// <summary>
/// Testes de integridade de dados para o módulo de Pedidos e Faturamento.
/// Épico #28: Auditoria de Testes — Vendas e Faturamento.
/// </summary>
public class PedidoIntegridadeTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Atualizar_Itens_Do_Pedido_Quando_Removidos_No_Omie()
    {
        var omieIdPedido = 555666L;
        var omieIdItem1 = 1L;
        var omieIdItem2 = 2L;
        long omieIdCliente = 777L;
        long omieIdProduto = 888L;

        // 1. Cria dependências e pedido no banco local
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var cliente = new Tabatine.Core.Entities.Cliente { Id = Guid.NewGuid(), OmieId = omieIdCliente, RazaoSocial = "Cliente Teste" };
            var produto = new Tabatine.Core.Entities.Produto { Id = Guid.NewGuid(), OmieId = omieIdProduto, CodigoProduto = "PROD001", Descricao = "Produto Teste" };
            dbContext.Clientes.Add(cliente);
            dbContext.Produtos.Add(produto);
            await dbContext.SaveChangesAsync();

            var pedido = new PedidoVenda 
            { 
                Id = Guid.NewGuid(), 
                OmieId = omieIdPedido, 
                NumeroPedido = "INT-001",
                Etapa = "10",
                ClienteId = cliente.Id
            };
            
            pedido.Itens.Add(new ItemPedido { Id = Guid.NewGuid(), OmieId = omieIdItem1, ProdutoId = produto.Id, Quantidade = 1, ValorUnitario = 100 });
            pedido.Itens.Add(new ItemPedido { Id = Guid.NewGuid(), OmieId = omieIdItem2, ProdutoId = produto.Id, Quantidade = 1, ValorUnitario = 100 });
            
            dbContext.PedidosVenda.Add(pedido);
            await dbContext.SaveChangesAsync();
        }

        // 2. Mock do Omie retorna apenas 1 item (o item 2 foi removido no ERP)
        var mockPedido = new OmiePedido
        {
            Cabecalho = new OmiePedidoCabecalho { CodigoPedido = omieIdPedido, NumeroPedido = "INT-001", Etapa = "10", CodigoCliente = omieIdCliente },
            Det = new List<OmiePedidoItem>
            {
                new() 
                { 
                    Ide = new OmiePedidoItemIde { CodigoItem = omieIdItem1 },
                    Produto = new OmiePedidoItemProduto { CodigoProduto = omieIdProduto, Quantidade = 1, ValorUnitario = 100 }
                }
            }
        };

        Factory.OmieClientMock.ConsultarPedidoAsync(omieIdPedido, Arg.Any<CancellationToken>())
            .Returns(mockPedido);

        // Act - Processa via Sync
        using (var scope = Factory.Services.CreateScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.PedidoSyncService>();
            await svc.SyncByIdAsync(omieIdPedido);
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pedido = await dbContext.PedidosVenda
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.OmieId == omieIdPedido);

            Assert.NotNull(pedido);
            Assert.Single(pedido!.Itens);
            Assert.DoesNotContain(pedido.Itens, i => i.OmieId == omieIdItem2);
        }
    }

    [Fact]
    public async Task Deve_Permitir_Criar_NotaFiscal_Mesmo_Se_Pedido_Ainda_Nao_Sincronizado()
    {
        // No OmieId pode acontecer da NF chegar via webhook antes do pedido.
        // O sistema deve ser resiliente e salvar a NF mesmo q o vínculo do pedido falhe momentaneamente (ou deixar nulo).
        
        var omieIdNf = 999555L; // ID único para evitar conflitos
        var omieIdPedidoNaoExistente = 888777L;
        long omieIdCliente = 444;
        
        var mockNf = new Tabatine.Omie.Client.Models.NotasFiscais.OmieNotaFiscal
        {
            Ide = new Tabatine.Omie.Client.Models.NotasFiscais.OmieNfIde 
            { 
                Numero = "999", 
                Serie = "1",
                DataEmissao = DateTime.UtcNow.ToString("dd/MM/yyyy"),
                Situacao = "100" // Autorizada
            },
            Compl = new Tabatine.Omie.Client.Models.NotasFiscais.OmieNfCompl
            {
                IdNf = omieIdNf,
                IdPedido = omieIdPedidoNaoExistente,
                XNatureza = "Venda de Mercadoria"
            },
            Destinatario = new Tabatine.Omie.Client.Models.NotasFiscais.OmieNfDestInt
            {
                CodigoCliente = omieIdCliente,
                RazaoSocial = "Cliente Teste NF"
            },
            Total = new Tabatine.Omie.Client.Models.NotasFiscais.OmieNfTotal
            {
                IcmsTot = new Tabatine.Omie.Client.Models.NotasFiscais.OmieNfIcmstot { ValorNota = 100 }
            }
        };

        Factory.OmieClientMock.ConsultarNotaFiscalAsync(omieIdNf, Arg.Any<CancellationToken>())
            .Returns(mockNf);

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Criar o cliente necessário para a NF
            dbContext.Clientes.Add(new Tabatine.Core.Entities.Cliente { Id = Guid.NewGuid(), OmieId = omieIdCliente, RazaoSocial = "Cliente Teste NF" });
            await dbContext.SaveChangesAsync();

            var svc = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.NotaFiscalSyncService>();
            // Simula processamento manual da NF
            await svc.SyncByIdAsync(omieIdNf);
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var nf = await dbContext.NotasFiscais.FirstOrDefaultAsync(n => n.OmieId == omieIdNf);

            Assert.NotNull(nf);
            Assert.Null(nf!.PedidoVendaId);
        }
    }
}
