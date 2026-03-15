using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class PedidoVenda : OmieEntityBase
    {
        public string NumeroPedido { get; set; } = string.Empty;
        public string Etapa { get; set; } = string.Empty; // Ex: FATURADO, CANCELADO
        public decimal ValorTotal { get; set; }
        public DateTime DataPrevisao { get; set; }
        
        // Relacionamento com Cliente
        public Guid ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        // Relacionamento 1:N com Itens do Pedido
        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
    }
}
