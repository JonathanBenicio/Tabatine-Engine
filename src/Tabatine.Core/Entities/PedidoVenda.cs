using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class PedidoVenda : OmieEntityBase
    {
        public string NumeroPedido { get; set; } = string.Empty;
        public string Etapa { get; set; } = string.Empty; // Ex: FATURADO, CANCELADO
        public decimal ValorTotal { get; set; }
        public DateTime? DataPrevisao { get; set; }
        
        // Frete e Logística
        public decimal ValorFrete { get; set; }
        public string? Transportadora { get; set; }
        public int QuantidadeVolumes { get; set; }

        // Condição de pagamento
        public string? CodigoParcela { get; set; }

        // Contato do pedido
        public string? Contato { get; set; }

        // Metadados
        public string? ObservacoesVenda { get; set; }
        public DateTime? DataInclusao { get; set; }
        public string? UsuarioInclusao { get; set; }
        public string? UsuarioAlteracao { get; set; }
        public bool Faturado { get; set; }
        public bool Cancelado { get; set; }
        public bool Devolvido { get; set; }
        public bool Autorizado { get; set; }
        public bool Denegado { get; set; }

        // Relacionamento com Cliente
        public Guid ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        // Relacionamento com Vendedor
        public Guid? VendedorId { get; set; }
        public Vendedor? Vendedor { get; set; }

        // Relacionamento com Conta Corrente
        public Guid? ContaCorrenteId { get; set; }
        public ContaCorrente? ContaCorrente { get; set; }

        // Relacionamento 1:N com Itens do Pedido
        public ICollection<ItemPedido> Itens { get; set; } = new List<ItemPedido>();

        // Relacionamento 1:N com Parcelas do Pedido
        public ICollection<PedidoParcela> Parcelas { get; set; } = new List<PedidoParcela>();
    }
}
