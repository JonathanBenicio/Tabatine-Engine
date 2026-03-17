using System;

namespace Tabatine.Core.Entities
{
    public class NotaFiscal : OmieEntityBase
    {
        public string NumeroNf { get; set; } = string.Empty;
        public string ChaveAcesso { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Autorizado, Cancelado
        public DateTime DataEmissao { get; set; }
        public TimeSpan? HoraEmissao { get; set; }
        public decimal ValorTotal { get; set; }

        // Impostos e Retenções
        public decimal ValorIss { get; set; }
        public decimal ValorIr { get; set; }
        public decimal ValorCsll { get; set; }
        public decimal ValorPisRetido { get; set; }
        public decimal ValorCofinsRetido { get; set; }

        public Guid? PedidoVendaId { get; set; }
        public PedidoVenda? PedidoVenda { get; set; }
        
        public Guid ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        public ICollection<ItemNotaFiscal> Itens { get; set; } = new List<ItemNotaFiscal>();
        public ICollection<NotaFiscalTitulo> Titulos { get; set; } = new List<NotaFiscalTitulo>();

        public Guid? VendedorId { get; set; }
        public Vendedor? Vendedor { get; set; }

        public Guid? ContaCorrenteId { get; set; }
        public ContaCorrente? ContaCorrente { get; set; }
    }
}
