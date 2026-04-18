using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tabatine.Omie.Client.Models;

namespace Tabatine.Worker.IntegrationTests.Helpers;

public class SandboxOmieHelper(string appKey, string appSecret, string baseUrl)
{
    private readonly HttpClient _httpClient = CreateHttpClient(baseUrl);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null // Mantém snake_case dos objetos anônimos
    };

    private static HttpClient CreateHttpClient(string baseUrl)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Tabatine-Engine-Tests");
        return client;
    }

    public async Task<long> UpsertClienteAsync(string razaoSocial, string documento)
    {
        if (string.IsNullOrWhiteSpace(appKey)) throw new ArgumentException("Sandbox AppKey is missing. Check appsettings.Local.json.");
        if (string.IsNullOrWhiteSpace(appSecret)) throw new ArgumentException("Sandbox AppSecret is missing. Check appsettings.Local.json.");

        var request = new OmieRequest<object>
        {
            AppKey = appKey,
            AppSecret = appSecret,
            Call = "UpsertCliente",
            Param = new List<object>
            {
                new
                {
                    codigo_cliente_integracao = Guid.NewGuid().ToString("N"),
                    razao_social = razaoSocial,
                    nome_fantasia = razaoSocial,
                    cnpj_cpf = documento
                }
            }
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        using var requestContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        // Força remover o charset caso a Omie seja sensível
        requestContent.Headers.ContentType!.CharSet = null;

        var response = await _httpClient.PostAsync("geral/clientes/", requestContent);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Omie Sandbox API returned {response.StatusCode}: {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        
        if (doc.RootElement.TryGetProperty("faultstring", out var fault))
        {
            throw new Exception($"Omie Fault: {fault.GetString()}");
        }

        return doc.RootElement.GetProperty("codigo_cliente_omie").GetInt64();
    }
}

public class OmieRequest<T>
{
    [JsonPropertyName("app_key")]
    public string AppKey { get; set; } = string.Empty;

    [JsonPropertyName("app_secret")]
    public string AppSecret { get; set; } = string.Empty;

    [JsonPropertyName("call")]
    public string Call { get; set; } = string.Empty;

    [JsonPropertyName("param")]
    public List<T> Param { get; set; } = new();
}
