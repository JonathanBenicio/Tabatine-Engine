using System;

namespace Tabatine.Core.Entities
{
    public class ItemNotaFiscal
    {
        public Guid Id { get; set; }
        public Guid NotaFiscalId { get; set; }
        public NotaFiscal NotaFiscal { get; set; } = null!;

        public Guid ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;

        public decimal Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        
        public string? Cfop { get; set; }
        public string? Ncm { get; set; }

        // Detalhamento de Impostos
        public decimal BaseIcms { get; set; }
        public decimal AliqIcms { get; set; }
        public string? CstIcms { get; set; }
        
        public decimal ValorIpi { get; set; }
        public decimal BaseIpi { get; set; }
        public decimal AliqIpi { get; set; }
        public string? CstIpi { get; set; }

        public decimal ValorPis { get; set; }
        public decimal ValorCofins { get; set; }

        // Novos Impostos (Reforma Tributária)
        public decimal ValorIbs { get; set; }
        public decimal AliqIbs { get; set; }
        public decimal ValorCbs { get; set; }
        public decimal AliqCbs { get; set; }
        public decimal BaseIbsCbs { get; set; }
    }
}
