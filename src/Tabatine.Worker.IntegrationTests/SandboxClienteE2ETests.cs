using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;
using Xunit;

namespace Tabatine.Worker.IntegrationTests.Services;

public class SandboxClienteE2ETests(SandboxIntegrationTestWebAppFactory factory) : BaseSandboxIntegrationTest(factory)
{
    [Fact]
    public async Task Deve_Sincronizar_Novo_Cliente_Do_Sandbox_Corretamente()
    {
        // 1. Arrange - Setup massa de dados no Sandbox Real
        var guid = Guid.NewGuid().ToString()[..8];
        var razaoSocial = $"TEST-E2E-SANDBOX-{guid}";
        var cnpj = "56272535000100"; // CNPJ Válido para teste
        
        var helper = CreateOmieHelper();
        var omieId = await helper.UpsertClienteAsync(razaoSocial, cnpj);

        using var scope = Factory.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<Tabatine.Infrastructure.Services.ClienteSyncService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 2. Act - Executar Sincronização
        // Sincronização Full (pelo menos inicial) para garantir que pegamos o novo item
        await syncService.SyncAllAsync(CancellationToken.None);

        // 3. Assert
        var clienteNoDb = await db.Clientes.FirstOrDefaultAsync(c => c.OmieId == omieId);
        
        clienteNoDb.Should().NotBeNull("O cliente criado na Sandbox deve ter sido sincronizado para o banco local.");
        clienteNoDb!.RazaoSocial.Should().Be(razaoSocial);

        // 4. Act 2 - Segunda sincronização (Incremental)
        // Deve registrar "0 sincronizados" ou simplesmente não duplicar nada
        var countAntes = await db.Clientes.CountAsync();
        await syncService.SyncAllAsync(CancellationToken.None);
        var countDepois = await db.Clientes.CountAsync();

        countDepois.Should().Be(countAntes, "Uma segunda sincronização imediata não deve inserir novos registros.");
    }
}
