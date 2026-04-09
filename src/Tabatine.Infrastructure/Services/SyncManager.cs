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
                typeof(EstoqueSyncService),
                typeof(PedidoSyncService),
                typeof(NotaFiscalSyncService),
                // Módulo Financeiro: executado após NF pois pode referenciar clientes e contas correntes
                typeof(ContasReceberSyncService),
                typeof(ContasPagarSyncService)
            };

                foreach (var type in serviceTypes)
                {
                    if (ct.IsCancellationRequested) break;

                    var retryCount = 0;
                    while (retryCount <= 1)
                    {
                        try
                        {
                            // Cria um scope novo para cada serviço de sync,
                            // garantindo um DbContext isolado por serviço.
                            using var scope = serviceProvider.CreateScope();
                            var service = (ISyncService)scope.ServiceProvider.GetRequiredService(type);
                            await service.SyncAllAsync(ct);
                            break; // Sucesso, sai do loop de retry
                        }
                        catch (HttpRequestException ex) when (ex.Message.Contains("REDUNDANT") || ex.Message.Contains("redundante"))
                        {
                            if (retryCount >= 1)
                            {
                                logger.LogWarning("Consumo redundante persistiu para {Service} após retry. Pulando para o próximo.", type.Name);
                                break;
                            }

                            logger.LogWarning("Consumo redundante detectado pela Omie para {Service}. Tentando novamente em 60s...", type.Name);
                            await Task.Delay(TimeSpan.FromSeconds(60), ct);
                            retryCount++;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Falha ao sincronizar serviço {Service}", type.Name);
                            break; // Outros erros não retentam no manager
                        }
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
        public async Task SyncByIdAsync(long omieId, CancellationToken ct = default)
        {
            // O SyncManager coordena o ciclo total. Sincronização por ID deve ser feita no serviço específico.
            await Task.CompletedTask;
        }
    }
}
