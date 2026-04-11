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
using Tabatine.Omie.Client.Models.Geral;
using Tabatine.Omie.Client.Models.Financeiro;
using Tabatine.Omie.Client.Models.Estoque;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
            _logger.LogDebug(">>> Enviando requisição Omie: {Url}. Call: {Call}. Payload: {Payload}", url, call, json);

            try
            {
                using var content = new StringContent(json, System.Text.Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                var response = await _httpClient.PostAsync(url, content, ct);
                var responseJson = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    if (responseJson.Contains("Client-5113") || responseJson.Contains("Client-101"))
                    {
                        _logger.LogDebug("<<< Omie retornou sem registros (5113/101) para {Url}. Call: {Call}", url, call);
                        return default!;
                    }

                    _logger.LogError("<<< Erro Omie {StatusCode} em {Url}. Call: {Call}. Resposta: {ErrorBody}", response.StatusCode, url, call, responseJson);
                    throw new HttpRequestException($"Erro Omie {response.StatusCode} em {call}: {responseJson}");
                }

                _logger.LogDebug("<<< Resposta Omie para {Call}: {Response}", call, responseJson);
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
            if (filtrarDe.HasValue)
            {
                param.FiltrarApenasAlteracao = "S";
                param.FiltrarPorDataDe = filtrarDe.Value.ToString("dd/MM/yyyy");
                param.FiltrarPorHoraDe = filtrarDe.Value.ToString("HH:mm:ss");
            }
            if (filtrarAte.HasValue)
            {
                param.FiltrarPorDataAte = filtrarAte.Value.ToString("dd/MM/yyyy");
                param.FiltrarPorHoraAte = filtrarAte.Value.ToString("HH:mm:ss");
            }
            return await SendRequestAsync<ListarClientesParam, ListarClientesResponse>("geral/clientes/", "ListarClientes", param, cancellationToken);
        }

        public async Task<ListarProdutosResponse> ListarProdutosAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarProdutosParam { Pagina = pagina };
            if (filtrarDe.HasValue)
            {
                param.FiltrarApenasAlteracao = "S";
                param.FiltrarPorDataDe = filtrarDe.Value.ToString("dd/MM/yyyy");
            }
            if (filtrarAte.HasValue) param.FiltrarPorDataAte = filtrarAte.Value.ToString("dd/MM/yyyy");
            return await SendRequestAsync<ListarProdutosParam, ListarProdutosResponse>("geral/produtos/", "ListarProdutos", param, cancellationToken);
        }

        public async Task<ListarPedidosResponse> ListarPedidosAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarPedidosParam { Pagina = pagina };
            if (filtrarDe.HasValue)
            {
                param.FiltrarApenasAlteracao = "S";
                param.FiltrarPorDataDe = filtrarDe.Value.ToString("dd/MM/yyyy");
            }
            if (filtrarAte.HasValue) param.FiltrarPorDataAte = filtrarAte.Value.ToString("dd/MM/yyyy");
            return await SendRequestAsync<ListarPedidosParam, ListarPedidosResponse>("produtos/pedido/", "ListarPedidos", param, cancellationToken);
        }

        public async Task<ListarNotasFiscaisResponse> ListarNotasFiscaisAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarNotasFiscaisParam { Pagina = pagina };
            if (filtrarDe.HasValue)
            {
                param.DataAlteracaoDe = filtrarDe.Value.ToString("dd/MM/yyyy");
                param.HoraAlteracaoDe = filtrarDe.Value.ToString("HH:mm:ss");
            }
            if (filtrarAte.HasValue)
            {
                param.DataEmissaoAte = filtrarAte.Value.ToString("dd/MM/yyyy");
            }
            return await SendRequestAsync<ListarNotasFiscaisParam, ListarNotasFiscaisResponse>("produtos/nfconsultar/", "ListarNF", param, cancellationToken);
        }

        public async Task<ListarVendedoresResponse> ListarVendedoresAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarVendedoresParam { Pagina = pagina };
            if (filtrarDe.HasValue)
            {
                param.FiltrarApenasAlteracao = "S";
                param.FiltrarPorDataDe = filtrarDe.Value.ToString("dd/MM/yyyy");
            }
            if (filtrarAte.HasValue) param.FiltrarPorDataAte = filtrarAte.Value.ToString("dd/MM/yyyy");

            return await SendRequestAsync<ListarVendedoresParam, ListarVendedoresResponse>("geral/vendedores/", "ListarVendedores", param, cancellationToken);
        }

        public async Task<ListarContaCorrenteResponse> ListarContasCorrentesAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default)
        {
            // Nota: o endpoint geral/contacorrente/ da Omie não suporta filtros de data.
            var param = new ListarContaCorrenteParam { Pagina = pagina };

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

        public async Task<MeiosPagamentoPesquisarResponse> ListarMeiosPagamentoAsync(CancellationToken cancellationToken = default)
        {
            var param = new MeiosPagamentoPesquisarRequest();
            return await SendRequestAsync<MeiosPagamentoPesquisarRequest, MeiosPagamentoPesquisarResponse>("geral/meiospagamento/", "ListarMeiosPagamento", param, cancellationToken);
        }

        public async Task<ListarParcelasResponse> ListarParcelasAsync(int pagina = 1, CancellationToken cancellationToken = default)
        {
            var param = new ListarParcelasParam { Pagina = pagina };
            return await SendRequestAsync<ListarParcelasParam, ListarParcelasResponse>("geral/parcelas/", "ListarParcelas", param, cancellationToken);
        }

        public async Task<ListarContasPagarResponse> ListarContasPagarAsync(int pagina = 1, long? codigoVendedor = null, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarContasPagarParam
            {
                Pagina = pagina,
                RegistrosPorPagina = 100 // Limite padrão para evitar 500 em algumas instâncias
            };

            // Removido filtrar_por_vendedor pois a Omie retorna 500 para este endpoint



            if (!string.IsNullOrEmpty(status))
                param.FiltrarPorStatus = status;

            if (filtrarDe.HasValue) param.FiltrarPorDataDe = filtrarDe.Value.ToString("dd/MM/yyyy");
            if (filtrarAte.HasValue) param.FiltrarPorDataAte = filtrarAte.Value.ToString("dd/MM/yyyy");

            return await SendRequestAsync<ListarContasPagarParam, ListarContasPagarResponse>("financas/contapagar/", "ListarContasPagar", param, cancellationToken);
        }

        public async Task<ListarContasReceberResponse> ListarContasReceberAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, CancellationToken cancellationToken = default)
        {
            var param = new ListarContasReceberParam
            {
                Pagina = pagina,
                RegistrosPorPagina = 100
            };


            if (!string.IsNullOrEmpty(status))
                param.FiltrarPorStatus = status;

            if (filtrarDe.HasValue) param.FiltrarPorDataDe = filtrarDe.Value.ToString("dd/MM/yyyy");
            if (filtrarAte.HasValue) param.FiltrarPorDataAte = filtrarAte.Value.ToString("dd/MM/yyyy");

            return await SendRequestAsync<ListarContasReceberParam, ListarContasReceberResponse>("financas/contareceber/", "ListarContasReceber", param, cancellationToken);
        }

        public async Task<Tabatine.Omie.Client.Models.Financeiro.ListarMovimentosResponse> ListarMovimentosFinanceirosAsync(int pagina = 1, long? nCodCC = null, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, CancellationToken cancellationToken = default)
        {
            var param = new Tabatine.Omie.Client.Models.Financeiro.ListarMovimentosParam
            {
                Pagina = pagina,
                RegistrosPorPagina = 100,
                CodigoContaCorrente = nCodCC,
                Status = status
            };

            if (filtrarDe.HasValue) param.DataAlteracaoDe = filtrarDe.Value.ToString("dd/MM/yyyy");
            if (filtrarAte.HasValue) param.DataAlteracaoAte = filtrarAte.Value.ToString("dd/MM/yyyy");

            return await SendRequestAsync<Tabatine.Omie.Client.Models.Financeiro.ListarMovimentosParam, Tabatine.Omie.Client.Models.Financeiro.ListarMovimentosResponse>("financas/mf/", "ListarMovimentos", param, cancellationToken);
        }

        public async Task<OmieCliente?> ConsultarClienteAsync(long codigoClienteOmie, CancellationToken cancellationToken = default)
        {
            var param = new { codigo_cliente_omie = codigoClienteOmie };
            return await SendRequestAsync<object, OmieCliente>("geral/clientes/", "ConsultarCliente", param, cancellationToken);
        }

        public async Task<OmieProduto?> ConsultarProdutoAsync(long codigoProdutoOmie, CancellationToken cancellationToken = default)
        {
            var param = new { codigo_produto = codigoProdutoOmie };
            return await SendRequestAsync<object, OmieProduto>("geral/produtos/", "ConsultarProduto", param, cancellationToken);
        }

        public async Task<OmiePedido?> ConsultarPedidoAsync(long codigoPedidoOmie, CancellationToken cancellationToken = default)
        {
            var param = new { codigo_pedido = codigoPedidoOmie };
            var response = await SendRequestAsync<object, OmiePedido>("produtos/pedido/", "ConsultarPedido", param, cancellationToken);
            return response;
        }

        public async Task<OmieNotaFiscal?> ConsultarNotaFiscalAsync(long codigoNfOmie, CancellationToken cancellationToken = default)
        {
            var param = new { codigo_nf = codigoNfOmie };
            var response = await SendRequestAsync<object, OmieNotaFiscal>("produtos/nfconsultar/", "ConsultarNF", param, cancellationToken);
            return response;
        }

        public async Task<OmieVendedor?> ConsultarVendedorAsync(long codigoVendedorOmie, CancellationToken cancellationToken = default)
        {
            var param = new { codigo = codigoVendedorOmie };
            return await SendRequestAsync<object, OmieVendedor>("geral/vendedores/", "ConsultarVendedor", param, cancellationToken);
        }

        public async Task<OmieContaPagar?> ConsultarContaPagarAsync(long codigoLancamentoOmie, CancellationToken cancellationToken = default)
        {
            var param = new { codigo_lancamento_omie = codigoLancamentoOmie };
            return await SendRequestAsync<object, OmieContaPagar>("financas/contapagar/", "ConsultarContaPagar", param, cancellationToken);
        }

        public async Task<OmieContaReceber?> ConsultarContaReceberAsync(long codigoLancamentoOmie, CancellationToken cancellationToken = default)
        {
            var param = new { codigo_lancamento_omie = codigoLancamentoOmie };
            return await SendRequestAsync<object, OmieContaReceber>("financas/contareceber/", "ConsultarContaReceber", param, cancellationToken);
        }

        // Streaming de Listagem

        public async IAsyncEnumerable<OmieCliente> StreamClientesAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarClientesAsync(current, filtrarDe, filtrarAte, cancellationToken);
                if (response == null || response.ClientesCadastro.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.ClientesCadastro) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieProduto> StreamProdutosAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarProdutosAsync(current, filtrarDe, filtrarAte, cancellationToken);
                if (response == null || response.ProdutosCadastro.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.ProdutosCadastro) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmiePedido> StreamPedidosAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarPedidosAsync(current, filtrarDe, filtrarAte, cancellationToken);
                if (response == null || response.PedidosVenda.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.PedidosVenda) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieNotaFiscal> StreamNotasFiscaisAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarNotasFiscaisAsync(current, filtrarDe, filtrarAte, cancellationToken);
                if (response == null || response.NotasFiscais.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.NotasFiscais) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieVendedor> StreamVendedoresAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarVendedoresAsync(current, filtrarDe, filtrarAte, cancellationToken);
                if (response == null || response.Vendedores.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.Vendedores) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieContaCorrente> StreamContasCorrentesAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarContasCorrentesAsync(current, filtrarDe, filtrarAte, cancellationToken);
                if (response == null || response.ContasCorrentes.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.ContasCorrentes) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieOperacaoEtapa> StreamEtapasFaturamentoAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarEtapasFaturamentoAsync(current, cancellationToken);
                if (response == null || response.Cadastros.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.Cadastros) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieFormaPagamento> StreamFormasPagVendasAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarFormasPagVendasAsync(current, cancellationToken);
                if (response == null || response.FormasPagamento.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.FormasPagamento) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieBanco> StreamBancosAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarBancosAsync(current, cancellationToken);
                if (response == null || response.Bancos.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.Bancos) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<ParcelaOmie> StreamParcelasAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarParcelasAsync(current, cancellationToken);
                if (response == null || response.Cadastros.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.Cadastros) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieContaPagar> StreamContasPagarAsync(long? codigoVendedor = null, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarContasPagarAsync(current, codigoVendedor, filtrarDe, filtrarAte, status, cancellationToken);
                if (response == null || response.ContasPagar.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.ContasPagar) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieContaReceber> StreamContasReceberAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarContasReceberAsync(current, filtrarDe, filtrarAte, status, cancellationToken);
                if (response == null || response.ContasReceber.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.ContasReceber) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<OmieMovimento> StreamMovimentosFinanceirosAsync(long? nCodCC = null, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var response = await ListarMovimentosFinanceirosAsync(current, nCodCC, filtrarDe, filtrarAte, status, cancellationToken);
                if (response == null || response.Movimentos.Count == 0) break;
                total = response.TotalDePaginas;
                foreach (var item in response.Movimentos) yield return item;
                current++;
            }
        }

        // Estoque

        public async IAsyncEnumerable<LocalEstoqueDto> ListarLocaisEstoqueAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var request = new ListarLocaisEstoqueRequest { Pagina = current, RegPorPagina = 100 };
                var response = await SendRequestAsync<ListarLocaisEstoqueRequest, ListarLocaisEstoqueResponse>("estoque/local/", "ListarLocaisEstoque", request, cancellationToken);
                total = response.TotPaginas;
                foreach (var item in response.Locais) yield return item;
                current++;
            }
        }

        public async Task<PosicaoEstoqueResponse> ConsultarPosicaoEstoqueAsync(PosicaoEstoqueRequest request, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<PosicaoEstoqueRequest, PosicaoEstoqueResponse>("estoque/consulta/", "PosicaoEstoque", request, cancellationToken);
        }

        public async IAsyncEnumerable<ProdutoEstoqueDto> StreamPosicaoEstoqueAsync(ListarPosEstoqueRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var req = request with { Pagina = current, RegPorPagina = 100 };
                var response = await SendRequestAsync<ListarPosEstoqueRequest, ListarPosEstoqueResponse>("estoque/consulta/", "ListarPosEstoque", req, cancellationToken);
                total = response.TotPaginas;
                foreach (var item in response.Produtos) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<MovimentoEstoqueDto> StreamMovimentoEstoqueAsync(ListarMovimentoEstoqueRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var req = request with { Pagina = current, RegPorPagina = 100 };
                var response = await SendRequestAsync<ListarMovimentoEstoqueRequest, ListarMovimentoEstoqueResponse>("estoque/consulta/", "ListarMovimentoEstoque", req, cancellationToken);
                total = response.TotPaginas;
                foreach (var item in response.Movimentos) yield return item;
                current++;
            }
        }

        public async IAsyncEnumerable<MovimentoProdutoDto> StreamMovimentosAsync(Tabatine.Omie.Client.Models.Estoque.ListarMovimentosRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int current = 1;
            int total = 1;
            while (current <= total)
            {
                var req = request with { Pagina = current, RegistrosPorPagina = 100 };
                var response = await SendRequestAsync<Tabatine.Omie.Client.Models.Estoque.ListarMovimentosRequest, Tabatine.Omie.Client.Models.Estoque.ListarMovimentosResponse>("estoque/movestoque/", "ListarMovimentos", req, cancellationToken);
                total = response.TotalPaginas;
                foreach (var item in response.Cadastros) yield return item;
                current++;
            }
        }

        public async Task<ObterEstoqueProdutoResponse> ObterResumoEstoqueProdutoAsync(ObterEstoqueProdutoRequest request, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<ObterEstoqueProdutoRequest, ObterEstoqueProdutoResponse>("estoque/resumo/", "ObterEstoqueProduto", request, cancellationToken);
        }
    }
}
