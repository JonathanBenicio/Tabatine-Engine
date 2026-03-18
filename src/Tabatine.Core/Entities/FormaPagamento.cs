using System;

namespace Tabatine.Core.Entities
{
    public class FormaPagamento : OmieEntityBase
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int QuantidadeParcelas { get; set; }
        public int? DiasParcelas { get; set; }
        public string? ListaParcelas { get; set; } // JSON ou string formatada
    }
}
