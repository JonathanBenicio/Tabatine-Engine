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
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pedidoDB = await dbContext.PedidosVenda
                .AsNoTracking()
                .Include(p => p.Itens)
                .Include(p => p.Parcelas)
                .FirstOrDefaultAsync(p => p.OmieId == omieIdPedido);
            
            if (pedidoDB != null && pedidoDB.Itens.Count > 0 && pedidoDB.Parcelas.Count > 0) break;
            await Task.Delay(500);
        }

        if (pedidoDB == null)
        {
            using var diagScope = Factory.Services.CreateScope();
            var dbDiag = diagScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var webhook = await dbDiag.WebhookEvents
                .AsNoTracking()
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(w => w.Event == "VendaProduto.Incluida");

            Assert.Fail($"Pedido {omieIdPedido} não persistido após {timeout.TotalSeconds}s. " +
                        $"Webhook status: {(webhook == null ? "Não encontrado" : (webhook.ProcessedAt.HasValue ? $"Processado em {webhook.ProcessedAt}" : $"Pendente/Erro (ID: {webhook.Id})"))}");
        }

        Assert.NotNull(pedidoDB);
        Assert.Equal(omiePedido.Cabecalho.NumeroPedido, pedidoDB!.NumeroPedido);
        Assert.Equal(1500.00m, pedidoDB.ValorTotal);
        
        // Validar Itens
        Assert.Single(pedidoDB.Itens);
        Assert.Equal(1500.00m, pedidoDB.Itens.First().ValorTotal);
        
        // Validar Parcelas
        Assert.Single(pedidoDB.Parcelas);
        Assert.Equal(1500.00m, pedidoDB.Parcelas.First().Valor);
    }

    [Fact]
    public async Task Deve_Processar_Upsert_Pedido_Com_Itens_DeletadosEAlterados()
    {
        // Arrange
        var omieIdPedido = 88776655L;
        var omieIdCliente = 12345L;
        var omieIdProduto1 = 54321L;
        var omieIdProduto2 = 12345L; // Produto que será deletado no update

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Pré-popula cenário: Um pedido com 2 itens
            var cliente = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieIdCliente);
            if (cliente == null)
            {
                cliente = new Cliente { Id = Guid.NewGuid(), OmieId = omieIdCliente, RazaoSocial = "Cliente Teste", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                dbContext.Clientes.Add(cliente);
            }
            
            var p1 = await dbContext.Produtos.FirstOrDefaultAsync(p => p.OmieId == omieIdProduto1) ?? new Produto { Id = Guid.NewGuid(), OmieId = omieIdProduto1, CodigoProduto="P1", Descricao="1", CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow };
            var p2 = await dbContext.Produtos.FirstOrDefaultAsync(p => p.OmieId == omieIdProduto2) ?? new Produto { Id = Guid.NewGuid(), OmieId = omieIdProduto2, CodigoProduto="P2", Descricao="2", CreatedAt=DateTime.UtcNow, UpdatedAt=DateTime.UtcNow };
            
            if (dbContext.Entry(p1).State == EntityState.Detached) dbContext.Produtos.Add(p1);
            if (dbContext.Entry(p2).State == EntityState.Detached) dbContext.Produtos.Add(p2);

            var pedidoOriginal = new PedidoVenda
            {
                Id = Guid.NewGuid(),
                OmieId = omieIdPedido,
                ClienteId = cliente.Id,
                NumeroPedido = "PED-UPDATE-001",
                ValorTotal = 2000.00m,
                Itens = new List<ItemPedido>
                {
                    new() { Id = Guid.NewGuid(), ProdutoId = p1.Id, Quantidade = 1, ValorUnitario = 1000, ValorTotal = 1000 },
                    new() { Id = Guid.NewGuid(), ProdutoId = p2.Id, Quantidade = 1, ValorUnitario = 1000, ValorTotal = 1000 }
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.PedidosVenda.Add(pedidoOriginal);
            await dbContext.SaveChangesAsync();
        }

        // Mock Omie return: Agora tem só 1 item, e o valor do Item 1 dobrou.
        var omiePedido = new OmiePedido
        {
            Cabecalho = new OmiePedidoCabecalho
            {
                CodigoPedido = omieIdPedido,
                NumeroPedido = "PED-UPDATE-001",
                CodigoCliente = omieIdCliente,
                Etapa = "20",
                QuantidadeItens = 1,
                DataPrevisao = "15/05/2026",
                QuantidadeParcelas = 1
            },
            TotalPedido = new OmiePedidoTotal { ValorTotalPedido = 2500.00m, ValorMercadorias = 2500.00m },
            Det = new List<OmiePedidoItem>
            {
                new() 
                {
                    Ide = new OmiePedidoItemIde { CodigoItem = 111222L },
                    Produto = new OmiePedidoItemProduto 
                    { 
                        CodigoProduto = omieIdProduto1, 
                        Quantidade = 2, 
                        ValorUnitario = 1250.00m,
                        ValorTotal = 2500.00m,
                        Unidade = "UN"
                    }
                }
            },
            ListaParcelas = new OmiePedidoParcelas { Parcelas = new() }
        };

        Factory.OmieClientMock.ConsultarPedidoAsync(omieIdPedido, Arg.Any<CancellationToken>())
            .Returns(omiePedido);

        var webhookEvent = new
        {
            topic = "VendaProduto.Alterada",
            messageId = Guid.NewGuid().ToString(),
            @event = new { idPedido = omieIdPedido, numeroPedido = "PED-UPDATE-001", etapa = "20" }
        };

        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        PedidoVenda? pedidoDB = null;
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            pedidoDB = await dbContext.PedidosVenda
                .AsNoTracking()
                .Include(p => p.Itens)
                .ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(p => p.OmieId == omieIdPedido);
            
            // Verifica se deletou 1 item e atualizou o total para 2500
            if (pedidoDB != null && pedidoDB.ValorTotal == 2500.00m && pedidoDB.Itens.Count == 1) break;
            await Task.Delay(500);
        }

        if (pedidoDB == null || pedidoDB.ValorTotal != 2500.00m)
        {
            using var diagScope = Factory.Services.CreateScope();
            var dbDiag = diagScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var webhook = await dbDiag.WebhookEvents
                .AsNoTracking()
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync(w => w.Event == "VendaProduto.Alterada");

            Assert.Fail($"Pedido {omieIdPedido} não atualizado após {timeout.TotalSeconds}s. " +
                        $"Webhook status: {(webhook == null ? "Não encontrado" : (webhook.ProcessedAt.HasValue ? $"Processado em {webhook.ProcessedAt}" : $"Pendente/Erro (ID: {webhook.Id})"))}");
        }

        Assert.NotNull(pedidoDB);
        Assert.Equal(2500.00m, pedidoDB!.ValorTotal);
        Assert.Single(pedidoDB.Itens);
        Assert.Equal(2500.00m, pedidoDB.Itens.First().ValorTotal);
    }
}
