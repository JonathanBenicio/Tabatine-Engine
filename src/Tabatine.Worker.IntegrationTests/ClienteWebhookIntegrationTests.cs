using Tabatine.Omie.Client.Models.Clientes;

namespace Tabatine.Worker.IntegrationTests;

public class ClienteWebhookIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Processar_Webhook_Cliente_E_Persistir_No_Banco_Com_Sucesso()
    {
        // Arrange
        var omieId = 12345678L;
        var omieCliente = new OmieCliente
        {
            CodigoClienteOmie = omieId,
            RazaoSocial = "Empresa de Teste Integado LTDA",
            NomeFantasia = "Teste Integrado",
            CnpjCpf = "12345678000199",
            Email = "contato@teste.com",
            Telefone = "1199999999",
            Cidade = "São Paulo",
            Estado = "SP",
            Endereco = "Rua dos Testes",
            EnderecoNumero = "100",
            Bairro = "Centro",
            Cep = "01001000",
            DAlt = "09/04/2026",
            HAlt = "10:00:00"
        };

        Factory.OmieClientMock.ConsultarClienteAsync(omieId, Arg.Any<CancellationToken>())
            .Returns(omieCliente);

        var webhookEvent = new
        {
            topic = "ClienteFornecedor.Incluido",
            messageId = Guid.NewGuid().ToString(),
            @event = new
            {
                idCliente = omieId,
                codigo_cliente_omie = omieId,
                razao_social = omieCliente.RazaoSocial
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/webhook/omie", webhookEvent);

        // Assert
        response.EnsureSuccessStatusCode();

        // Polling
        await Task.Delay(1000);

        // Polling para aguardar o processamento assíncrono (Worker)
        Cliente? clienteDB = null;
        var timeout = TimeSpan.FromSeconds(10);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clienteDB = await dbContext.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieId);
            
            if (clienteDB != null) break;
            await Task.Delay(1000); 
        }

        clienteDB.Should().NotBeNull("O cliente deve ser persistido no banco de dados");
        clienteDB!.RazaoSocial.Should().Be(omieCliente.RazaoSocial);
        clienteDB.CnpjCpf.Should().Be(omieCliente.CnpjCpf);
        clienteDB.Cidade.Should().Be("São Paulo");
    }
}
