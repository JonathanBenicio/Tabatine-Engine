using System;

namespace Tabatine.Core.Entities
{
    public class NotaFiscal : OmieEntityBase
    {
        public string NumeroNf { get; set; } = string.Empty;
        public string ChaveAcesso { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Autorizado, Cancelado, Denegada
        public int CodigoStatus { get; set; } // 100, 101, 110, 301
        public DateTime DataEmissao { get; set; }
        public TimeSpan? HoraEmissao { get; set; }
        public decimal ValorTotal { get; set; }
        public string? NaturezaOperacao { get; set; }
        public string? Serie { get; set; }
        public string? Modelo { get; set; } = "55";
        public bool ImportadoApi { get; set; } = true;

        // Impostos e Retenções
        public decimal ValorIss { get; set; }
        public decimal ValorIr { get; set; }
        public decimal ValorCsll { get; set; }
        public decimal ValorPisRetido { get; set; }
        public decimal ValorCofinsRetido { get; set; }

        public decimal ValorIpi { get; set; }
        public decimal ValorPis { get; set; }
        public decimal ValorCofins { get; set; }
        public decimal ValorProd { get; set; }
        public decimal IcmsBaseCalculo { get; set; }
        public decimal IcmsValor { get; set; }
        public bool Denegada { get; set; }

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
