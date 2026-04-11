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
            var config = services
                .Where(d => d.ServiceType == typeof(IConfiguration))
                .Select(d => d.ImplementationInstance)
                .OfType<IConfiguration>()
                .FirstOrDefault();

            if (config == null) return;

            var appKey = config["Omie:AppKey_Sandbox"];
            var appSecret = config["Omie:AppSecret_Sandbox"];
            var baseUrl = config["Omie:BaseUrl_Sandbox"];

            if (string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret))
            {
                return;
            }

            services.RemoveAll<IOmieClient>();

            services.AddHttpClient<IOmieClient, OmieClient>(client =>
            {
                client.BaseAddress = new Uri(baseUrl ?? "https://app.omie.com.br/api/v1/");
            });

            services.Configure<OmieOptions>(options =>
            {
                options.AppKey = appKey;
                options.AppSecret = appSecret;
                options.BaseUrl = baseUrl ?? "https://app.omie.com.br/api/v1/";
            });
        });
    }
}
