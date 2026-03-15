using Microsoft.Extensions.DependencyInjection;
using Tabatine.Core.Interfaces;

namespace Tabatine.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();
                try 
                {
                    await syncService.SyncAllAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante a sincronização.");
                }
            }

            // Aguarda 1 hora entre sincronizações
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
