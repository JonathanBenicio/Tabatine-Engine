using System.Text.Json.Serialization;
using System.Collections.Generic;
using Tabatine.Omie.Client.Models;

namespace Tabatine.Omie.Client.Models.Financeiro
{
    public class ListarContasReceberParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("filtrar_por_status")]
        public string? FiltrarPorStatus { get; set; }

        [JsonPropertyName("exibir_obs")]
        public string ExibirObs { get; set; } = "S";
    }

    public class OmieContaReceber
    {
        [JsonPropertyName("codigo_lancamento_omie")]
        public long CodigoLancamentoOmie { get; set; }

        [JsonPropertyName("codigo_cliente_fornecedor")]
        public long CodigoClienteFornecedor { get; set; }

        [JsonPropertyName("numero_pedido")]
        public string? NumeroPedido { get; set; }

        [JsonPropertyName("numero_parcela")]
        public string? NumeroParcela { get; set; }

        [JsonPropertyName("numero_documento")]
        public string? NumeroDocumento { get; set; }

        [JsonPropertyName("data_emissao")]
        public string DataEmissao { get; set; } = string.Empty;

        [JsonPropertyName("data_vencimento")]
        public string DataVencimento { get; set; } = string.Empty;

        [JsonPropertyName("data_previsao")]
        public string? DataPrevisao { get; set; }

        [JsonPropertyName("valor_documento")]
        public decimal ValorDocumento { get; set; }

        [JsonPropertyName("valor_recebido")]
        public decimal ValorRecebido { get; set; }

        [JsonPropertyName("valor_saldo")]
        public decimal ValorSaldo { get; set; }

        [JsonPropertyName("status_titulo")]
        public string? StatusTitulo { get; set; }

        [JsonPropertyName("data_baixa")]
        public string? DataBaixa { get; set; }

        [JsonPropertyName("codigo_vendedor")]
        public long? CodigoVendedor { get; set; }

        [JsonPropertyName("codigo_conta_corrente")]
        public long? CodigoContaCorrente { get; set; }

        [JsonPropertyName("codigo_categoria")]
        public string? CodigoCategoria { get; set; }

        [JsonPropertyName("observacao")]
        public string? Observacao { get; set; }

        [JsonPropertyName("info")]
        public OmieContaReceberInfo? Info { get; set; }
    }

    public class OmieContaReceberInfo
    {
        [JsonPropertyName("dInc")]
        public string? DInc { get; set; }

        [JsonPropertyName("hInc")]
        public string? HInc { get; set; }

        [JsonPropertyName("dAlt")]
        public string? DAlt { get; set; }

        [JsonPropertyName("hAlt")]
        public string? HAlt { get; set; }
    }

    public class ListarContasReceberResponse : OmieResponse<OmieContaReceber>
    {
        [JsonPropertyName("conta_receber_cadastro")]
        public List<OmieContaReceber> ContasReceber { get; set; } = new();
    }
}
