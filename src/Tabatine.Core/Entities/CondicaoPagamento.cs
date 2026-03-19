using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class CondicaoPagamento : OmieEntityBase
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int QuantidadeParcelas { get; set; }
        public int DiaFixo { get; set; }

        public ICollection<PedidoVenda> PedidosVenda { get; set; } = new List<PedidoVenda>();
    }
}
