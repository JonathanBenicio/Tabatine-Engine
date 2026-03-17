using System;

namespace Tabatine.Core.Entities
{
    public class NotaFiscalTitulo
    {
        public Guid Id { get; set; }
        public Guid NotaFiscalId { get; set; }
        public NotaFiscal NotaFiscal { get; set; } = null!;

        public int NumeroParcela { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        
        // ID do título na Omie (opcional, para reconciliação)
        public long? OmieIdTitulo { get; set; }

        public Guid? ContaCorrenteId { get; set; }
        public ContaCorrente? ContaCorrente { get; set; }

        // Vendedor responsável pelo título (vem do campo nCodVendedor da Omie)
        public Guid? VendedorId { get; set; }
        public Vendedor? Vendedor { get; set; }
    }
}
