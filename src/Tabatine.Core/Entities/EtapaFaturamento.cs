using System;

namespace Tabatine.Core.Entities
{
    public class EtapaFaturamento : OmieEntityBase
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string? DescricaoPadrao { get; set; }
        public bool Inativa { get; set; }
        
        // As etapas são agrupadas por operação (ex: "Venda de Produtos")
        public string CodigoOperacao { get; set; } = string.Empty;
        public string DescricaoOperacao { get; set; } = string.Empty;
    }
}
