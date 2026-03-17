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
    }
}
