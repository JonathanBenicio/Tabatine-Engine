using System;

namespace Tabatine.Core.Entities
{
    public class ItemPedido : OmieEntityBase
    {
        public Guid PedidoVendaId { get; set; }
        public PedidoVenda PedidoVenda { get; set; } = null!;
        
        public Guid ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;
        
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
