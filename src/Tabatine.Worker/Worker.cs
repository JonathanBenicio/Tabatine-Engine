using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Tabatine.Core.Interfaces;

namespace Tabatine.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<int>("SyncIntervalInMinutes", 60);
        _logger.LogInformation("Worker configurado para rodar a cada {Minutes} minutos.", intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Iniciando ciclo de sincronização em: {time}", DateTimeOffset.Now);
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

            _logger.LogInformation("Aguardando {Minutes} minutos para o próximo ciclo...", intervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
        }
    }
}
