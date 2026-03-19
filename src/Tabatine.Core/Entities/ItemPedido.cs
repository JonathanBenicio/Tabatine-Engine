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
        public string? UnidadeMedida { get; set; }
        
        // Impostos e Descontos
        public decimal ValorIcms { get; set; }
        public decimal ValorIpi { get; set; }
        public decimal ValorPis { get; set; }
        public decimal ValorCofins { get; set; }
        public decimal PercentualDesconto { get; set; }
        public decimal ValorDesconto { get; set; }

        // Detalhamento de Impostos (Bases e Alíquotas)
        public decimal BaseIcms { get; set; }
        public decimal AliqIcms { get; set; }
        public string? CstIcms { get; set; }

        public decimal BaseIpi { get; set; }
        public decimal AliqIpi { get; set; }
        public string? CstIpi { get; set; }

        public decimal BasePis { get; set; }
        public decimal AliqPis { get; set; }
        public string? CstPis { get; set; }

        public decimal BaseCofins { get; set; }
        public decimal AliqCofins { get; set; }
        public string? CstCofins { get; set; }

        // Pesos
        public decimal PesoBruto { get; set; }
        public decimal PesoLiquido { get; set; }
    }
}
