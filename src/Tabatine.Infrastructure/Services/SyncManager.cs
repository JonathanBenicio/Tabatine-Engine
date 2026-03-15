using Tabatine.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tabatine.Infrastructure.Services
{
    public class SyncManager : ISyncService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyncManager> _logger;

        public SyncManager(IServiceProvider serviceProvider, ILogger<SyncManager> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Iniciando Ciclo de Sincronização Global...");

            // Ordem de faturamento: Clientes -> Produtos -> Pedidos/Vendas -> Notas Fiscais
            
            var serviceTypes = new[] 
            { 
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
                    var service = (ISyncService)_serviceProvider.GetRequiredService(type);
                    await service.SyncAllAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao sincronizar serviço {Service}", type.Name);
                }
            }

            _logger.LogInformation("Ciclo de Sincronização Global finalizado.");
        }
    }
}
