using System;

namespace Tabatine.Core.Entities
{
    public class PedidoParcela
    {
        public Guid Id { get; set; }
        public Guid PedidoVendaId { get; set; }
        public PedidoVenda PedidoVenda { get; set; } = null!;

        public int NumeroParcela { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public decimal Percentual { get; set; }

        // Conta Corrente onde esta parcela será recebida
        public Guid? ContaCorrenteId { get; set; }
        public ContaCorrente? ContaCorrente { get; set; }
    }
}
