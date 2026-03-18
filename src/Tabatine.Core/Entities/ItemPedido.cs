using System;

namespace Tabatine.Core.Entities
{
    public class ItemPedido : OmieEntityBase
    {
        public Guid PedidoVendaId { get; set; }
        public PedidoVenda PedidoVenda { get; set; } = null!;
        
        public Guid ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;

        public Guid? TabelaPrecoId { get; set; }
        public TabelaPreco? TabelaPreco { get; set; }
        
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        
        // Impostos e Descontos
        public decimal ValorIcms { get; set; }
        public decimal ValorIpi { get; set; }
        public decimal ValorPis { get; set; }
        public decimal ValorCofins { get; set; }
        public decimal PercentualDesconto { get; set; }
        public decimal ValorDesconto { get; set; }

        // Pesos
        public decimal PesoBruto { get; set; }
        public decimal PesoLiquido { get; set; }
    }
}
