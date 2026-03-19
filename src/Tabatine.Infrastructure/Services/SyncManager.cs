using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Interfaces;

namespace Tabatine.Infrastructure.Services
{
    public class SyncManager(IServiceProvider serviceProvider, ILogger<SyncManager> logger, IDistributedLockService lockService) : ISyncService
    {
        private const string LockKey = "tabatine:sync:lock";

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            var lockToken = Guid.NewGuid().ToString();
            
            // Tenta obter o lock por 30 minutos (tempo máximo de um ciclo)
            if (!await lockService.TryAcquireLockAsync(LockKey, lockToken, TimeSpan.FromMinutes(30), ct))
            {
                logger.LogWarning("Ciclo de sincronização já está em execução em outro worker. Abortando.");
                return;
            }

      try
      {
        logger.LogInformation("Iniciando Ciclo de Sincronização Global...");

        // Ordem de faturamento: Clientes -> Produtos -> Pedidos/Vendas -> Notas Fiscais

        var serviceTypes = new[]
        { 
                typeof(BancoSyncService),
                typeof(MeioPagamentoSyncService),
                typeof(EtapaFaturamentoSyncService),
                typeof(FormaPagamentoSyncService),
                typeof(CondicaoPagamentoSyncService),
                typeof(VendedorSyncService),
                typeof(ContaCorrenteSyncService),
                typeof(ClienteSyncService), 
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
            using var scope = serviceProvider.CreateScope();
            var service = (ISyncService)scope.ServiceProvider.GetRequiredService(type);
            await service.SyncAllAsync(ct);
          }
          catch (HttpRequestException ex) when (ex.Message.Contains("REDUNDANT"))
          {
            logger.LogWarning("Consumo redundante detectado pela Omie para {Service}. Aguardando 30s antes de continuar...", type.Name);
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
          }
          catch (Exception ex)
          {
            logger.LogError(ex, "Falha ao sincronizar serviço {Service}", type.Name);
          }

          // Aguarda entre serviços para evitar "consumo redundante" na Omie
          if (!ct.IsCancellationRequested)
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
      }

      finally
      {
        await lockService.ReleaseLockAsync(LockKey, lockToken, ct);
        logger.LogInformation("Ciclo de Sincronização Global finalizado e trava liberada.");
      }
    }
    }
}
