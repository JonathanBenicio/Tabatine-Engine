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
using System;
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
        Task<OmieCliente?> ConsultarClienteAsync(long codigoClienteOmie, CancellationToken cancellationToken = default);
        Task<OmiePedido?> ConsultarPedidoAsync(long codigoPedidoOmie, CancellationToken cancellationToken = default);
        Task<OmieNotaFiscal?> ConsultarNotaFiscalAsync(long codigoNfOmie, CancellationToken cancellationToken = default);
    }
}
