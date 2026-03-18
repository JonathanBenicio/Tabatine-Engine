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
        public decimal PesoBruto { get; set; }
        public decimal PesoLiquido { get; set; }
        public DateTime? PrevisaoEntrega { get; set; }

        // Impostos Totais
        public decimal ValorIcms { get; set; }
        public decimal ValorIpi { get; set; }
        public decimal ValorPis { get; set; }
        public decimal ValorCofins { get; set; }
        public decimal BaseCalculoIcms { get; set; }
        public decimal ValorMercadorias { get; set; }
        
        // Retenções no Pedido
        public decimal ValorIss { get; set; }
        public decimal ValorIr { get; set; }
        public decimal ValorCsll { get; set; }
        public decimal ValorInss { get; set; }
        
        public decimal ComissaoVendedor { get; set; }
        public string? FreteModalidade { get; set; }

        // Condição de pagamento
        public string? CodigoParcela { get; set; }

        // Contato do pedido
        public string? Contato { get; set; }

        // Metadados
        public string? ObservacoesVenda { get; set; }
        public string? ObservacoesInternas { get; set; }
        public string? MeioPagamento { get; set; }
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

        // Relacionamento com Etapa de Faturamento
        public Guid? EtapaFaturamentoId { get; set; }
        public EtapaFaturamento? EtapaFaturamento { get; set; }

        // Relacionamento com Forma de Pagamento
        public Guid? FormaPagamentoId { get; set; }
        public FormaPagamento? FormaPagamento { get; set; }

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
