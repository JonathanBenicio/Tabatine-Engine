namespace Tabatine.Worker.IntegrationTests;

public class PedidoWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_Pedido_E_Persistir_Completo_No_Banco()
    {
        // Arrange
        var omieIdPedido = 99887766L;
        var omieIdCliente = 12345L;
        var omieIdProduto = 54321L;

        // 1. Seed de Cliente e Produto no Banco (necessários para a sincronização do Pedido)
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var cliente = new Cliente 
            { 
                Id = Guid.NewGuid(), 
                OmieId = omieIdCliente, 
                RazaoSocial = "Cliente do Pedido",
                CnpjCpf = "00000000000100",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            var produto = new Produto 
            { 
                Id = Guid.NewGuid(), 
                OmieId = omieIdProduto, 
                Descricao = "Produto do Pedido",
                CodigoProduto = "PROD001",
                UnidadeMedida = "UN",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Clientes.Add(cliente);
            dbContext.Produtos.Add(produto);
            await dbContext.SaveChangesAsync();
        }

        // 2. Mock do Pedido na API Omie
        var omiePedido = new OmiePedido
        {
            Cabecalho = new OmiePedidoCabecalho
            {
                CodigoPedido = omieIdPedido,
                NumeroPedido = "PED-INT-001",
                CodigoCliente = omieIdCliente,
                Etapa = "10", // Novo
                QuantidadeItens = 1,
                DataPrevisao = "15/05/2026",
                QuantidadeParcelas = 1
            },
            TotalPedido = new OmiePedidoTotal
            {
                ValorTotalPedido = 1500.00m,
                ValorMercadorias = 1500.00m
            },
            Det = new List<OmiePedidoItem>
            {
                new() 
                {
                    Ide = new OmiePedidoItemIde { CodigoItem = 111222L },
                    Produto = new OmiePedidoItemProduto 
                    { 
                        CodigoProduto = omieIdProduto, 
                        Quantidade = 1, 
                        ValorUnitario = 1500.00m,
                        ValorTotal = 1500.00m,
                        Unidade = "UN"
                    }
                }
            },
            ListaParcelas = new OmiePedidoParcelas
            {
                Parcelas = new List<OmiePedidoParcela>
                {
                    new()
                    {
                        NumeroParcela = 1,
                        Valor = 1500.00m,
                        DataVencimento = "15/06/2026",
                        Percentual = 100
                    }
                }
            },
            InfoCadastro = new OmieInfoCadastro
            {
                DInc = "09/04/2026",
                HInc = "14:00:00",
                DAlt = "09/04/2026",
                HAlt = "14:00:00"
            }
        };

        Factory.OmieClientMock.ConsultarPedidoAsync(omieIdPedido, Arg.Any<CancellationToken>())
            .Returns(omiePedido);

        // 3. Payload do Webhook
        var webhookEvent = new
        {
            topic = "VendaProduto.Incluida",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                idPedido = omieIdPedido,
                numeroPedido = omiePedido.Cabecalho.NumeroPedido,
                etapa = omiePedido.Cabecalho.Etapa
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        // Polling para aguardar o processamento assíncrono
        PedidoVenda? pedidoDB = null;
        var timeout = TimeSpan.FromSeconds(15);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pedidoDB = await dbContext.PedidosVenda
                .Include(p => p.Itens)
                .Include(p => p.Parcelas)
                .FirstOrDefaultAsync(p => p.OmieId == omieIdPedido);
            
            if (pedidoDB != null && pedidoDB.Itens.Count > 0 && pedidoDB.Parcelas.Count > 0) break;
            await Task.Delay(500);
        }

        pedidoDB.Should().NotBeNull("O pedido deve ser persistido no banco de dados");
        pedidoDB!.NumeroPedido.Should().Be(omiePedido.Cabecalho.NumeroPedido);
        pedidoDB.ValorTotal.Should().Be(1500.00m);
        
        // Validar Itens
        pedidoDB.Itens.Should().HaveCount(1);
        pedidoDB.Itens.First().ValorTotal.Should().Be(1500.00m);
        
        // Validar Parcelas
        pedidoDB.Parcelas.Should().HaveCount(1);
        pedidoDB.Parcelas.First().Valor.Should().Be(1500.00m);
    }
}
