using System.Net.Http.Json;
using System.Text.Json;
using Tabatine.Omie.Client.Models;

namespace Tabatine.Worker.IntegrationTests.Helpers;

public class SandboxOmieHelper(string appKey, string appSecret, string baseUrl)
{
    private readonly HttpClient _httpClient = new() { BaseAddress = new Uri(baseUrl) };

    public async Task<long> UpsertClienteAsync(string razaoSocial, string documento)
    {
        var request = new OmieRequest<object>
        {
            AppKey = appKey,
            AppSecret = appSecret,
            Call = "UpsertCliente",
            Param = new List<object>
            {
                new
                {
                    codigo_cliente_integracao = Guid.NewGuid().ToString(),
                    razao_social = razaoSocial,
                    cnpj_cpf = documento,
                    nome_fantasia = "TEST-E2E"
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync("geral/clientes/", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("codigo_cliente_omie").GetInt64();
    }
}

public class OmieRequest<T>
{
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string Call { get; set; } = string.Empty;
    public List<T> Param { get; set; } = new();
}
