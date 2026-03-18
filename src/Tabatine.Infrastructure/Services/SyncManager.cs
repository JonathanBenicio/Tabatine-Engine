using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Tabatine.Core.Interfaces;

namespace Tabatine.Infrastructure.Services
{
    public class SyncManager : ISyncService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyncManager> _logger;
        private readonly IConnectionMultiplexer _redis;
        private const string LockKey = "tabatine:sync:lock";

        public SyncManager(IServiceProvider serviceProvider, ILogger<SyncManager> logger, IConnectionMultiplexer redis)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _redis = redis;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            var db = _redis.GetDatabase();
            var lockToken = Guid.NewGuid().ToString();
            
            // Tenta obter o lock por 30 minutos (tempo máximo de um ciclo)
            if (!await db.LockTakeAsync(LockKey, lockToken, TimeSpan.FromMinutes(30)))
            {
                _logger.LogWarning("Ciclo de sincronização já está em execução em outro worker. Abortando.");
                return;
            }

      try
      {
        _logger.LogInformation("Iniciando Ciclo de Sincronização Global...");

        // Ordem de faturamento: Clientes -> Produtos -> Pedidos/Vendas -> Notas Fiscais

        var serviceTypes = new[]
        { 
                typeof(BancoSyncService),
                typeof(EtapaFaturamentoSyncService),
                typeof(FormaPagamentoSyncService),
                typeof(ClienteSyncService), 
                typeof(VendedorSyncService),
                typeof(ContaCorrenteSyncService),
                typeof(ProdutoSyncService),
                typeof(PedidoSyncService),
                typeof(NotaFiscalSyncService)
            };

        foreach (var type in serviceTypes)
        {
          if (ct.IsCancellationRequested) break;

          try
          {
            // Cria um scope novo para cada serviço de sync,
            // garantindo um DbContext isolado por serviço.
            using var scope = _serviceProvider.CreateScope();
            var service = (ISyncService)scope.ServiceProvider.GetRequiredService(type);
            await service.SyncAllAsync(ct);
          }
          catch (HttpRequestException ex) when (ex.Message.Contains("REDUNDANT"))
          {
            _logger.LogWarning("Consumo redundante detectado pela Omie para {Service}. Aguardando 30s antes de continuar...", type.Name);
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Falha ao sincronizar serviço {Service}", type.Name);
          }

          // Aguarda entre serviços para evitar "consumo redundante" na Omie
          if (!ct.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
      }

      finally
      {
        await db.LockReleaseAsync(LockKey, lockToken);
        _logger.LogInformation("Ciclo de Sincronização Global finalizado e trava liberada.");
      }
    }
    }
}
