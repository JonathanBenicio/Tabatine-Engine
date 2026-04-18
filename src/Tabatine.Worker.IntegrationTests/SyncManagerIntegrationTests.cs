using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Diagnostics;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;

namespace Tabatine.Worker.IntegrationTests.Services;

public class SyncManagerIntegrationTests : BaseIntegrationTest
{
    public SyncManagerIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Deve_Executar_Rotina_Completa_De_Sincronizacao_Com_Sucesso()
    {
        // 1. Arrange
        using var scope = Factory.Services.CreateScope();
        var syncManager = scope.ServiceProvider.GetRequiredService<ISyncService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Os mocks do OmieClient por padrão retornarão null para as listagens (ListarBancosAsync, ListarClientesAsync, etc.)
        // Isso simula respostas vazias ou listas vazias, o que é suficiente para garantir
        // que todos os serviços estão devidamente registrados na DI e não estouram NullReferenceExceptions 
        // ou dependências circulares.

        // Limpar possíveis execuções passadas nos testes (Garantir que a tabela está limpa)
        await db.Database.ExecuteSqlRawAsync("DELETE FROM integration_sync_states;");

        // 2. Act
        var stopwatch = Stopwatch.StartNew();
        
        // Chamamos a execução ponta-a-ponta.
        // O timeout de cancellation está desabilitado aqui para permitir o fluxo todo (Pode levar alguns segundos devido aos Delays do Worker)
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300)); 
        await syncManager.SyncAllAsync(cts.Token);
        
        stopwatch.Stop();

        // 3. Assert (Sempre validando o estado do Banco via um novo Scope caso o context anterior feche)
        using var assertScope = Factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var syncStates = await assertDb.IntegrationSyncStates.ToListAsync();

        // Deve haver estados de sync salvos para todos os módulos configurados (aprox. 14 no ciclo)
        Assert.NotEmpty(syncStates);
        
        // O mínimo esperado é > 10 (considerando que são 14 módulos ao todo)
        Assert.True(syncStates.Count > 10, "Todos os serviços atrelados ao SyncManager deveriam ter registrado sua execução no SyncState.");

        // Validando módulos vitais
        var modulos = syncStates.Select(s => s.ModuleName).ToList();
        Assert.Contains("Bancos", modulos);
        Assert.Contains("Clientes", modulos);
        Assert.Contains("Pedidos", modulos);
        Assert.Contains("NotasFiscais", modulos);
        Assert.Contains("Estoque", modulos);

        // Todos os módulos listados não devem ter falhado (LastSyncDate deve refletir agora)
        foreach (var state in syncStates)
        {
            Assert.True(Math.Abs((state.LastSyncDate - DateTime.UtcNow).TotalMinutes) < 2);
        }
    }
}
