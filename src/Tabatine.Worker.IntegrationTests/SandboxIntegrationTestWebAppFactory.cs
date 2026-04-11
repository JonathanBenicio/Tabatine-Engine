using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tabatine.Omie.Client;
using Tabatine.Omie.Client.Models;
using Tabatine.Worker.Extensions;

namespace Tabatine.Worker.IntegrationTests;

public class SandboxIntegrationTestWebAppFactory : IntegrationTestWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Chama a base para garantir que o Testcontainers (Postgres) seja configurado
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            // Resolvemos o IConfiguration dentro do setup de serviços
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IConfiguration>();

            var appKey = config["Omie:AppKey_Sandbox"];
            var appSecret = config["Omie:AppSecret_Sandbox"];
            var baseUrl = config["Omie:BaseUrl_Sandbox"];

            if (string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret))
            {
                // Se não houver chaves de sandbox, não sobrescrevemos para deixar o teste falhar ou ser skipado no nível do teste
                return;
            }

            // Remove o mock registrado pela base
            services.RemoveAll<IOmieClient>();

            // Registra o OmieClient real com as chaves de Sandbox
            services.AddHttpClient<IOmieClient, OmieClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl ?? "https://app.omie.com.br/api/v1/");
            });

            // Sobrescrevemos as opções especificamente para este container
            // Isso garante que o OmieClient (que usa IOptions<OmieOptions>) pegue as chaves certas
            services.Configure<OmieOptions>(options =>
            {
                options.AppKey = appKey;
                options.AppSecret = appSecret;
                options.BaseUrl = baseUrl ?? "https://app.omie.com.br/api/v1/";
            });
        });
    }
}
