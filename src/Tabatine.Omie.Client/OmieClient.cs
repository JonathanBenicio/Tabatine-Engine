using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Tabatine.Omie.Client.Models;
using Tabatine.Omie.Client.Models.Clientes;
using Tabatine.Omie.Client.Models.Produtos;
using Tabatine.Omie.Client.Models.Pedidos;
using Tabatine.Omie.Client.Models.NotasFiscais;

namespace Tabatine.Omie.Client
{
    public class OmieClient : IOmieClient
    {
        private readonly HttpClient _httpClient;
        private readonly OmieOptions _options;

        public OmieClient(HttpClient httpClient, IOptions<OmieOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<ListarClientesResponse> ListarClientesAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var request = new OmieRequest<ListarClientesParam>
            {
                AppKey = _options.AppKey,
                AppSecret = _options.AppSecret,
                Call = "ListarClientes",
                Param = new List<ListarClientesParam> { new ListarClientesParam { Pagina = pagina } }
            };

            var response = await _httpClient.PostAsJsonAsync("geral/clientes/", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<ListarClientesResponse>(cancellationToken: cancellationToken))!;
        }

        public async Task<ListarProdutosResponse> ListarProdutosAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var request = new OmieRequest<ListarProdutosParam>
            {
                AppKey = _options.AppKey,
                AppSecret = _options.AppSecret,
                Call = "ListarProdutos",
                Param = new List<ListarProdutosParam> { new ListarProdutosParam { Pagina = pagina } }
            };

            var response = await _httpClient.PostAsJsonAsync("geral/produtos/", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<ListarProdutosResponse>(cancellationToken: cancellationToken))!;
        }

        public async Task<ListarPedidosResponse> ListarPedidosAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var request = new OmieRequest<ListarPedidosParam>
            {
                AppKey = _options.AppKey,
                AppSecret = _options.AppSecret,
                Call = "ListarPedidos",
                Param = new List<ListarPedidosParam> { new ListarPedidosParam { Pagina = pagina } }
            };

            var response = await _httpClient.PostAsJsonAsync("produtos/pedido/", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<ListarPedidosResponse>(cancellationToken: cancellationToken))!;
        }

        public async Task<ListarNotasFiscaisResponse> ListarNotasFiscaisAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var request = new OmieRequest<ListarNotasFiscaisParam>
            {
                AppKey = _options.AppKey,
                AppSecret = _options.AppSecret,
                Call = "ListarNF",
                Param = new List<ListarNotasFiscaisParam> { new ListarNotasFiscaisParam { Pagina = pagina } }
            };

            var response = await _httpClient.PostAsJsonAsync("produtos/nfconsultar/", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<ListarNotasFiscaisResponse>(cancellationToken: cancellationToken))!;
        }
    }
}
