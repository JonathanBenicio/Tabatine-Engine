using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Tabatine.Omie.Client.Models;
using Tabatine.Omie.Client.Models.Clientes;
using Tabatine.Omie.Client.Models.Produtos;
using Tabatine.Omie.Client.Models.Pedidos;
using Tabatine.Omie.Client.Models.NotasFiscais;
using Tabatine.Omie.Client.Models.Vendedores;
using Tabatine.Omie.Client.Models.ContaCorrente;
using Tabatine.Omie.Client.Models.EtapaFaturamento;
using Tabatine.Omie.Client.Models.FormaPagamento;
using Tabatine.Omie.Client.Models.Bancos;

namespace Tabatine.Omie.Client
{
    public class OmieClient : IOmieClient
    {
        private readonly HttpClient _httpClient;
        private readonly OmieOptions _options;
        private readonly ILogger<OmieClient> _logger;

        public OmieClient(HttpClient httpClient, IOptions<OmieOptions> options, ILogger<OmieClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(string url, string call, TRequest param, CancellationToken ct)
            where TRequest : class
            where TResponse : class
        {
            var request = new OmieRequest<TRequest>
            {
                AppKey = _options.AppKey,
                AppSecret = _options.AppSecret,
                Call = call,
                Param = new List<TRequest> { param }
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogDebug("Enviando requisição Omie para {Url}. Call: {Call}. Payload: {Payload}", url, call, json);

            try
            {
                using var content = new StringContent(json, System.Text.Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                
                var response = await _httpClient.PostAsync(url, content, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(ct);
                    
                    if (errorBody.Contains("Client-5113"))
                    {
                        _logger.LogInformation("Omie retornou sem registros para {Url}. Call: {Call}. (Client-5113 - sem dados para o filtro)", url, call);
                        return default!;
                    }
                    
                    _logger.LogError("Erro Omie {StatusCode} em {Url}. Call: {Call}. Resposta: {ErrorBody}", response.StatusCode, url, call, errorBody);
                    throw new HttpRequestException($"Erro Omie {response.StatusCode} em {call}: {errorBody}");
                }

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions)!;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timeout ou Cancelamento na requisição Omie para {Url}. Call: {Call}. Verifique as políticas de resiliência.", url, call);
                throw;
            }
        }

        public async Task<ListarClientesResponse> ListarClientesAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarClientesParam { Pagina = pagina };
            if (filtrarDe.HasValue || filtrarAte.HasValue)
            {
                param.FiltrarPorDataDe = filtrarDe?.ToString("dd/MM/yyyy");
                param.FiltrarPorDataAte = filtrarAte?.ToString("dd/MM/yyyy");
            }

            return await SendRequestAsync<ListarClientesParam, ListarClientesResponse>("geral/clientes/", "ListarClientes", param, cancellationToken);
        }

        public async Task<ListarProdutosResponse> ListarProdutosAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarProdutosParam { Pagina = pagina };
            if (filtrarDe.HasValue || filtrarAte.HasValue)
            {
                param.FiltrarPorDataDe = filtrarDe?.ToString("dd/MM/yyyy");
                param.FiltrarPorDataAte = filtrarAte?.ToString("dd/MM/yyyy");
            }

            return await SendRequestAsync<ListarProdutosParam, ListarProdutosResponse>("geral/produtos/", "ListarProdutos", param, cancellationToken);
        }

        public async Task<ListarPedidosResponse> ListarPedidosAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarPedidosParam { Pagina = pagina };
            if (filtrarDe.HasValue || filtrarAte.HasValue)
            {
                param.FiltrarPorDataDe = filtrarDe?.ToString("dd/MM/yyyy");
                param.FiltrarPorDataAte = filtrarAte?.ToString("dd/MM/yyyy");
            }

            return await SendRequestAsync<ListarPedidosParam, ListarPedidosResponse>("produtos/pedido/", "ListarPedidos", param, cancellationToken);
        }

        public async Task<ListarNotasFiscaisResponse> ListarNotasFiscaisAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarNotasFiscaisParam { Pagina = pagina };
            if (filtrarDe.HasValue || filtrarAte.HasValue)
            {
                param.FiltrarPorDataDe = filtrarDe?.ToString("dd/MM/yyyy");
                param.FiltrarPorDataAte = filtrarAte?.ToString("dd/MM/yyyy");
            }

            return await SendRequestAsync<ListarNotasFiscaisParam, ListarNotasFiscaisResponse>("produtos/nfconsultar/", "ListarNF", param, cancellationToken);
        }

        public async Task<ListarVendedoresResponse> ListarVendedoresAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarVendedoresParam { Pagina = pagina };
            if (filtrarDe.HasValue || filtrarAte.HasValue)
            {
                param.FiltrarPorDataDe = filtrarDe?.ToString("dd/MM/yyyy");
                param.FiltrarPorDataAte = filtrarAte?.ToString("dd/MM/yyyy");
            }

            return await SendRequestAsync<ListarVendedoresParam, ListarVendedoresResponse>("geral/vendedores/", "ListarVendedores", param, cancellationToken);
        }

        public async Task<ListarContaCorrenteResponse> ListarContasCorrentesAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarContaCorrenteParam { Pagina = pagina };
            if (filtrarDe.HasValue || filtrarAte.HasValue)
            {
                param.FiltrarPorDataDe = filtrarDe?.ToString("dd/MM/yyyy");
                param.FiltrarPorDataAte = filtrarAte?.ToString("dd/MM/yyyy");
            }

            return await SendRequestAsync<ListarContaCorrenteParam, ListarContaCorrenteResponse>("geral/contacorrente/", "ListarContasCorrentes", param, cancellationToken);
        }

        public async Task<ListarEtapasFaturamentoResponse> ListarEtapasFaturamentoAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var param = new ListarEtapasFaturamentoParam { Pagina = pagina };
            return await SendRequestAsync<ListarEtapasFaturamentoParam, ListarEtapasFaturamentoResponse>("produtos/etapafat/", "ListarEtapasFaturamento", param, cancellationToken);
        }

        public async Task<ListarFormasPagVendasResponse> ListarFormasPagVendasAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var param = new ListarFormasPagVendasParam { Pagina = pagina };
            return await SendRequestAsync<ListarFormasPagVendasParam, ListarFormasPagVendasResponse>("produtos/formaspagvendas/", "ListarFormasPagVendas", param, cancellationToken);
        }

        public async Task<ListarBancosResponse> ListarBancosAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var param = new ListarBancosParam { Pagina = pagina };
            return await SendRequestAsync<ListarBancosParam, ListarBancosResponse>("geral/bancos/", "ListarBancos", param, cancellationToken);
        }
    }
}
