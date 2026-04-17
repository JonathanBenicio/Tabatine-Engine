using Tabatine.Omie.Client.Models.NotasFiscais;

namespace Tabatine.Worker.IntegrationTests;

public class NotaFiscalWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_NotaFiscal_E_Persistir_No_Banco_Com_Sucesso()
    {
        // Arrange
        var omieIdNf = 987654321L;
        var omieIdCliente = 12345L;

        // 1. Garantir que o cliente e pedido existem (necessário para o SyncService da NF)
        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                OmieId = omieIdCliente,
                RazaoSocial = "Cliente Teste NF",
                CnpjCpf = "00000000000100",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var pedido = new PedidoVenda
            {
                Id = Guid.NewGuid(),
                OmieId = 88888L,
                ClienteId = cliente.Id,
                NumeroPedido = "PED-TEST",
                ValorTotal = 1500.50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            dbContext.Clientes.Add(cliente);
            dbContext.PedidosVenda.Add(pedido);
            await dbContext.SaveChangesAsync();
        }

        // 2. Mock da resposta da Omie para consulta individual
        var omieNf = new OmieNotaFiscal
        {
            Compl = new OmieNfCompl { IdNf = omieIdNf, IdPedido = 88888L, ChaveNfe = "35230400000000000000000000000000000000000000", XNatureza = "Venda de Mercadoria" },
            Ide = new OmieNfIde { Numero = "1234", Serie = "1", DataEmissao = "10/04/2026", Situacao = "100" },
            Destinatario = new OmieNfDestInt { CodigoCliente = omieIdCliente },
            Total = new OmieNfTotal { IcmsTot = new OmieNfIcmstot { ValorNota = 1500.50m } }
        };

        Factory.OmieClientMock.ConsultarNotaFiscalAsync(omieIdNf, Arg.Any<CancellationToken>())
            .Returns(omieNf);

        var statusPedidoResponse = new Tabatine.Omie.Client.Models.Pedidos.StatusPedidoResponse
        {
            CodigoPedido = 88888L,
            ListaNfe = new List<Tabatine.Omie.Client.Models.Pedidos.StatusPedidoNfe>
            {
                new Tabatine.Omie.Client.Models.Pedidos.StatusPedidoNfe
                {
                    ChaveNfe = "35230400000000000000000000000000000000000000",
                    Danfe = "https://app.omie.com.br/api/v1/produtos/nfconsultar/danfe/?codigo_nf=987654321"
                }
            }
        };

        Factory.OmieClientMock.StatusPedidoAsync(88888L, Arg.Any<CancellationToken>())
            .Returns(statusPedidoResponse);

        // 3. Payload do Webhook (Simulando Connect 2.0)
        var webhookEvent = new
        {
            topic = "NFe.NotaAutorizada",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                idNf = omieIdNf,
                numeroNf = "1234"
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        // Polling para aguardar o processamento assíncrono (Worker)
        NotaFiscal? nfDB = null;
        var timeout = TimeSpan.FromSeconds(20);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            nfDB = await dbContext.NotasFiscais
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.OmieId == omieIdNf);
            
            if (nfDB != null) break;
            await Task.Delay(500);
        }

        Assert.NotNull(nfDB);
        Assert.Equal("1234", nfDB!.NumeroNf);
        Assert.Equal(1500.50m, nfDB.ValorTotal);
        Assert.Equal(omieNf.Compl.ChaveNfe, nfDB.ChaveAcesso);
        Assert.Equal("https://app.omie.com.br/api/v1/produtos/nfconsultar/danfe/?codigo_nf=987654321", nfDB.LinkDanfe);
    }

    [Fact]
    public async Task Deve_Rejeitar_Ou_Tratar_NotaFiscal_Sem_Pedido_Com_Sucesso()
    {
        // Arrange
        var omieIdNf = 111111L;
        var omieIdCliente = 12345L;
        var omieIdPedidoInexistente = 9999999L; // Pedido não existe no BD

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cliente = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieIdCliente);
            if (cliente == null)
            {
                cliente = new Cliente { Id = Guid.NewGuid(), OmieId = omieIdCliente, RazaoSocial = "Cliente NF Sem Pedido", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                dbContext.Clientes.Add(cliente);
                await dbContext.SaveChangesAsync();
            }
        }

        var omieNf = new OmieNotaFiscal
        {
            Compl = new OmieNfCompl { IdNf = omieIdNf, IdPedido = omieIdPedidoInexistente, ChaveNfe = "111", XNatureza = "Venda" },
            Ide = new OmieNfIde { Numero = "5555", Serie = "1", DataEmissao = "10/04/2026", Situacao = "100" },
            Destinatario = new OmieNfDestInt { CodigoCliente = omieIdCliente },
            Total = new OmieNfTotal { IcmsTot = new OmieNfIcmstot { ValorNota = 500.00m } }
        };

        Factory.OmieClientMock.ConsultarNotaFiscalAsync(omieIdNf, Arg.Any<CancellationToken>())
            .Returns(omieNf);

        var webhookEvent = new
        {
            topic = "NFe.NotaAutorizada",
            messageId = Guid.NewGuid().ToString(),
            @event = new { idNf = omieIdNf, numeroNf = "5555" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);
        response.EnsureSuccessStatusCode();

        NotaFiscal? nfDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            nfDB = await dbContext.NotasFiscais.FirstOrDefaultAsync(n => n.OmieId == omieIdNf);
            
            if (nfDB != null) break;
            await Task.Delay(500);
        }

        // Se a NF for inserida com Pedido nulo, nfDb não pode estar nula.
        // Se a NF deve falhar por Constraint FK, nfDb ficará nula, e o Log/DLQ conterá o registro.
        // Isso depende da FK no EFCore para PedidoVenda. 
        // Comumente, sincronizações marcam o PedidoId local como NULO.
        Assert.NotNull(nfDB);
        Assert.Null(nfDB!.PedidoVendaId);
    }
}
