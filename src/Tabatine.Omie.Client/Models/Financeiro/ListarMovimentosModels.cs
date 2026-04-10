using System.Text.Json.Serialization;
using System.Collections.Generic;
using Tabatine.Omie.Client.Models;

namespace Tabatine.Omie.Client.Models.Financeiro
{
    public class ListarMovimentosParam
    {
        [JsonPropertyName("nPagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("nRegPorPagina")]
        public int RegistrosPorPagina { get; set; } = 500;

        [JsonPropertyName("dDtAltDe")]
        public string? DataAlteracaoDe { get; set; }

        [JsonPropertyName("dDtAltAte")]
        public string? DataAlteracaoAte { get; set; }

        [JsonPropertyName("nCodCC")]
        public long? CodigoContaCorrente { get; set; }

        [JsonPropertyName("cStatus")]
        public string? Status { get; set; }

        [JsonPropertyName("lDadosCad")]
        public string DadosCadastrais { get; set; } = "S";
    }

    public class OmieMovimento
    {
        [JsonPropertyName("detalhes")]
        public MovimentoDetalhes Detalhes { get; set; } = new();

        [JsonPropertyName("resumo")]
        public MovimentoResumo Resumo { get; set; } = new();
    }

    public class MovimentoDetalhes
    {
        [JsonPropertyName("nCodTitulo")]
        public long CodigoTitulo { get; set; }

        [JsonPropertyName("cNumTitulo")]
        public string? NumeroTitulo { get; set; }

        [JsonPropertyName("dDtEmissao")]
        public string? DataEmissao { get; set; }

        [JsonPropertyName("dDtVenc")]
        public string? DataVencimento { get; set; }

        [JsonPropertyName("dDtPagamento")]
        public string? DataPagamento { get; set; }

        [JsonPropertyName("nCodCliente")]
        public long CodigoCliente { get; set; }

        [JsonPropertyName("nCodCC")]
        public long CodigoContaCorrente { get; set; }

        [JsonPropertyName("cStatus")]
        public string? Status { get; set; }

        [JsonPropertyName("cNatureza")]
        public string? Natureza { get; set; } // "R" (Receita) ou "D" (Despesa)

        [JsonPropertyName("cTipo")]
        public string? Tipo { get; set; }

        [JsonPropertyName("nValorTitulo")]
        public decimal ValorTitulo { get; set; }

        [JsonPropertyName("dDtConcilia")]
        public string? DataConciliacao { get; set; }

        [JsonPropertyName("nCodMovCC")]
        public long? CodigoMovimentoCC { get; set; }
    }

    public class MovimentoResumo
    {
        [JsonPropertyName("cLiquidado")]
        public string? Liquidado { get; set; } // "S" ou "N"

        [JsonPropertyName("nValPago")]
        public decimal ValorPago { get; set; }

        [JsonPropertyName("nValAberto")]
        public decimal ValorAberto { get; set; }

        [JsonPropertyName("nDesconto")]
        public decimal Desconto { get; set; }

        [JsonPropertyName("nJuros")]
        public decimal Juros { get; set; }

        [JsonPropertyName("nMulta")]
        public decimal Multa { get; set; }

        [JsonPropertyName("nValLiquido")]
        public decimal ValorLiquido { get; set; }
    }

    public class ListarMovimentosResponse : OmieResponse<OmieMovimento>
    {
        [JsonPropertyName("movimentos")]
        public List<OmieMovimento> Movimentos { get; set; } = new();
    }
}
