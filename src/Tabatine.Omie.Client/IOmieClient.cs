using Tabatine.Omie.Client.Models.Clientes;
using Tabatine.Omie.Client.Models.Produtos;
using Tabatine.Omie.Client.Models.Pedidos;
using Tabatine.Omie.Client.Models.NotasFiscais;
using Tabatine.Omie.Client.Models.Vendedores;
using Tabatine.Omie.Client.Models.ContaCorrente;

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
    }
}
