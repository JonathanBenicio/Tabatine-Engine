using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class ContaCorrente : OmieEntityBase
    {
        public string Descricao { get; set; } = string.Empty;
        public string? CodigoIntegracao { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public bool Inativa { get; set; }
        public decimal SaldoInicial { get; set; }

        // Relacionamento com Banco
        public Guid? BancoId { get; set; }
        public Banco? Banco { get; set; }

        public ICollection<PedidoVenda> PedidosVenda { get; set; } = new List<PedidoVenda>();
        public ICollection<NotaFiscal> NotasFiscais { get; set; } = new List<NotaFiscal>();
        public ICollection<PedidoParcela> Parcelas { get; set; } = new List<PedidoParcela>();
        public ICollection<NotaFiscalTitulo> Titulos { get; set; } = new List<NotaFiscalTitulo>();
    }
}
