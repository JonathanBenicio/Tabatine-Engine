using System;

namespace Tabatine.Core.Entities
{
    public class Produto : OmieEntityBase
    {
        public string CodigoProduto { get; set; } = string.Empty; // SKU ou Código interno
        public string Descricao { get; set; } = string.Empty;
        public string Ncm { get; set; } = string.Empty;
        public string? Ean { get; set; }
        public string? UnidadeMedida { get; set; }
        public decimal PesoLiquido { get; set; }
        public decimal PesoBruto { get; set; }
        public string? FamiliaProduto { get; set; }
        public decimal PrecoUnitario { get; set; }
        public bool Ativo { get; set; }
    }
}
