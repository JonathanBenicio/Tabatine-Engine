using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class Vendedor : OmieEntityBase
    {
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public decimal Comissao { get; set; }
        public bool Inativo { get; set; }

        public ICollection<PedidoVenda> PedidosVenda { get; set; } = new List<PedidoVenda>();
        public ICollection<NotaFiscal> NotasFiscais { get; set; } = new List<NotaFiscal>();
        public ICollection<NotaFiscalTitulo> Titulos { get; set; } = new List<NotaFiscalTitulo>();
    }
}
