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
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            var appKey = GetConfigValue(config, "Omie:AppKey_Sandbox", "Omie:Sandbox:AppKey");
            var appSecret = GetConfigValue(config, "Omie:AppSecret_Sandbox", "Omie:Sandbox:AppSecret");
            var baseUrl = GetConfigValue(config, "Omie:BaseUrl_Sandbox", "Omie:Sandbox:BaseUrl");

            if (string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret))
            {
                return;
            }

            // Remove o Mock que o base.ConfigureWebHost adicionou
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

    private static string? GetConfigValue(IConfiguration config, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = config[key];
            if (!string.IsNullOrEmpty(value) && value != "Use-Environment-Variable")
            {
                return value;
            }
        }
        return null;
    }
}
