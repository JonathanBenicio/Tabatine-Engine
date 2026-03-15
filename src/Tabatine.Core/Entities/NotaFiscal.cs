using System;

namespace Tabatine.Core.Entities
{
    public class NotaFiscal : OmieEntityBase
    {
        public string NumeroNf { get; set; } = string.Empty;
        public string ChaveAcesso { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Autorizado, Cancelado
        public DateTime DataEmissao { get; set; }
        public decimal ValorTotal { get; set; }

        public Guid? PedidoVendaId { get; set; }
        public PedidoVenda? PedidoVenda { get; set; }
        
        public Guid ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;
    }
}
