namespace Tabatine.Core.Entities
{
    public class TituloPagar : OmieEntityBase
    {
        // Identificação do título na Omie
        public string NumeroDocumento { get; set; } = string.Empty;
        public string? NumeroParcela { get; set; }
        public string? NumeroPedido { get; set; }

        // Datas do ciclo de vida financeiro
        public DateTime DataEmissao { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataBaixa { get; set; }

        // Valores
        public decimal ValorDocumento { get; set; }
        public decimal ValorPago { get; set; }
        public decimal ValorSaldo { get; set; }

        // Status Omie: "APAGAR" | "PAGO" | "ATRASADO"
        public string? StatusTitulo { get; set; }

        // Categoria financeira do Omie
        public string? CodigoCategoria { get; set; }

        public string? Observacao { get; set; }

        // Fornecedor = Cliente com flag EhFornecedor (cadastro unificado Omie)
        public Guid? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public Guid? ContaCorrenteId { get; set; }
        public ContaCorrente? ContaCorrente { get; set; }
    }
}
