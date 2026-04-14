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
            // Resolve a configuração atualizada (incluindo appsettings.Local.json)
            var sp = services.BuildServiceProvider();
            var config = sp.GetRequiredService<IConfiguration>();

            var appKey = config["Omie:Sandbox:AppKey"];
            var appSecret = config["Omie:Sandbox:AppSecret"];
            var baseUrl = config["Omie:Sandbox:BaseUrl"];

            if (string.IsNullOrEmpty(appKey) || string.IsNullOrEmpty(appSecret))
            {
                // Se não houver chaves de sandbox, mantém o comportamento de mock da base (opcional)
                // ou lança erro se o teste for explicitamente de sandbox
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
