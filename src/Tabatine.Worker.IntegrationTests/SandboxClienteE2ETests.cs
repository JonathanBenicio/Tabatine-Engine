using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Xunit;

namespace Tabatine.Worker.IntegrationTests.Services;

[Trait("Category", "Sandbox")]
public class SandboxClienteE2ETests(SandboxIntegrationTestWebAppFactory factory) : BaseSandboxIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Sincronizar_Novo_Cliente_Do_Sandbox_Corretamente()
    {
        // 1. Arrange - Setup massa de dados no Sandbox Real
        var guid = Guid.NewGuid().ToString()[..8];
        var razaoSocial = $"TEST-E2E-SANDBOX-{guid}";
        var cnpj = "33.000.167/0001-01"; // CNPJ Válido (Petrobras) com pontuação
        
        var helper = CreateOmieHelper();
        var omieId = await helper.UpsertClienteAsync(razaoSocial, cnpj);

        // Pequeno delay para garantir consistência na Omie (Sandbox pode ser lento)
        await Task.Delay(2000);

        using var scope = Factory.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ClienteSyncService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 2. Act - Executar Sincronização Individual (Mais confiável que Listar logo após Insert)
        await syncService.SyncByIdAsync(omieId, CancellationToken.None);

        // 3. Assert
        var clienteNoDb = await db.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieId);
        
        Assert.NotNull(clienteNoDb);
        Assert.Equal(razaoSocial, clienteNoDb!.RazaoSocial);

        // 4. Act 2 - Sincronização em Lote (deve manter o estado ou atualizar se a Omie já indexou)
        await syncService.SyncAllAsync(CancellationToken.None);
        
        var clienteAindaNoDb = await db.Clientes.AnyAsync(c => c.OmieId == omieId);
        Assert.True(clienteAindaNoDb);
    }
}
