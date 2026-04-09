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
using System.Threading;
using System.Threading.Tasks;

namespace Tabatine.Omie.Client
{
    public interface IOmieClient
    {
        Task<ListarClientesResponse> ListarClientesAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        Task<ListarProdutosResponse> ListarProdutosAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        Task<ListarPedidosResponse> ListarPedidosAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        Task<ListarNotasFiscaisResponse> ListarNotasFiscaisAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);   
        Task<ListarVendedoresResponse> ListarVendedoresAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);       
        Task<ListarContaCorrenteResponse> ListarContasCorrentesAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        Task<ListarEtapasFaturamentoResponse> ListarEtapasFaturamentoAsync(int pagina = 1, CancellationToken cancellationToken = default);
        Task<ListarFormasPagVendasResponse> ListarFormasPagVendasAsync(int pagina = 1, CancellationToken cancellationToken = default);
        Task<ListarBancosResponse> ListarBancosAsync(int pagina = 1, CancellationToken cancellationToken = default);
        Task<MeiosPagamentoPesquisarResponse> ListarMeiosPagamentoAsync(CancellationToken cancellationToken = default);
        Task<ListarParcelasResponse> ListarParcelasAsync(int pagina = 1, CancellationToken cancellationToken = default);
        Task<ListarContasPagarResponse> ListarContasPagarAsync(int pagina = 1, long? codigoVendedor = null, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, CancellationToken cancellationToken = default);
        Task<ListarContasReceberResponse> ListarContasReceberAsync(int pagina = 1, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, CancellationToken cancellationToken = default);
        Task<OmieCliente?> ConsultarClienteAsync(long codigoClienteOmie, CancellationToken cancellationToken = default);
        Task<OmieProduto?> ConsultarProdutoAsync(long codigoProdutoOmie, CancellationToken cancellationToken = default);
        Task<OmiePedido?> ConsultarPedidoAsync(long codigoPedidoOmie, CancellationToken cancellationToken = default);
        Task<OmieNotaFiscal?> ConsultarNotaFiscalAsync(long codigoNfOmie, CancellationToken cancellationToken = default);
        Task<OmieVendedor?> ConsultarVendedorAsync(long codigoVendedorOmie, CancellationToken cancellationToken = default);

        // Streaming de Listagem
        IAsyncEnumerable<OmieCliente> StreamClientesAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieProduto> StreamProdutosAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmiePedido> StreamPedidosAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieNotaFiscal> StreamNotasFiscaisAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieVendedor> StreamVendedoresAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieContaCorrente> StreamContasCorrentesAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieOperacaoEtapa> StreamEtapasFaturamentoAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieFormaPagamento> StreamFormasPagVendasAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieBanco> StreamBancosAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<ParcelaOmie> StreamParcelasAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieContaPagar> StreamContasPagarAsync(long? codigoVendedor = null, DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<OmieContaReceber> StreamContasReceberAsync(DateTime? filtrarDe = null, DateTime? filtrarAte = null, string? status = null, CancellationToken cancellationToken = default);

        // Estoque
        IAsyncEnumerable<LocalEstoqueDto> ListarLocaisEstoqueAsync(CancellationToken cancellationToken = default);
        Task<PosicaoEstoqueResponse> ConsultarPosicaoEstoqueAsync(PosicaoEstoqueRequest request, CancellationToken cancellationToken = default);
        IAsyncEnumerable<ProdutoEstoqueDto> StreamPosicaoEstoqueAsync(ListarPosEstoqueRequest request, CancellationToken cancellationToken = default);
        IAsyncEnumerable<MovimentoEstoqueDto> StreamMovimentoEstoqueAsync(ListarMovimentoEstoqueRequest request, CancellationToken cancellationToken = default);
        IAsyncEnumerable<MovimentoProdutoDto> StreamMovimentosAsync(ListarMovimentosRequest request, CancellationToken cancellationToken = default);
        Task<ObterEstoqueProdutoResponse> ObterResumoEstoqueProdutoAsync(ObterEstoqueProdutoRequest request, CancellationToken cancellationToken = default);
    }
}
