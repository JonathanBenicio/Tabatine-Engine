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
    }
}
