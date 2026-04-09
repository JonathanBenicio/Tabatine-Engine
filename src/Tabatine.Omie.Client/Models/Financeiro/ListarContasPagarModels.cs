using System.Text.Json.Serialization;
using System.Collections.Generic;
using Tabatine.Omie.Client.Models;

namespace Tabatine.Omie.Client.Models.Financeiro
{
    public class ListarContasPagarParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 500;

        [JsonPropertyName("filtrar_por_data_de")]
        public string? FiltrarPorDataDe { get; set; }

        [JsonPropertyName("filtrar_por_data_ate")]
        public string? FiltrarPorDataAte { get; set; }

        [JsonPropertyName("filtrar_por_status")]
        public string? FiltrarPorStatus { get; set; }

        [JsonPropertyName("filtrar_por_vendedor")]
        public long? FiltrarPorVendedor { get; set; }

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";
        
        [JsonPropertyName("exibir_obs")]
        public string ExibirObs { get; set; } = "S";
    }

    public class OmieContaPagar
    {
        [JsonPropertyName("codigo_lancamento_omie")]
        public long CodigoLancamentoOmie { get; set; }

        [JsonPropertyName("codigo_cliente_fornecedor")]
        public long CodigoClienteFornecedor { get; set; }

        [JsonPropertyName("data_vencimento")]
        public string DataVencimento { get; set; } = string.Empty;

        [JsonPropertyName("valor_documento")]
        public decimal ValorDocumento { get; set; }

        [JsonPropertyName("codigo_categoria")]
        public string? CodigoCategoria { get; set; }

        [JsonPropertyName("numero_pedido")]
        public string? NumeroPedido { get; set; }

        [JsonPropertyName("numero_documento")]
        public string? NumeroDocumento { get; set; }

        [JsonPropertyName("observacao")]
        public string? Observacao { get; set; }

        [JsonPropertyName("codigo_vendedor")]
        public long? CodigoVendedor { get; set; }

        [JsonPropertyName("codigo_conta_corrente")]
        public long? CodigoContaCorrente { get; set; }

        [JsonPropertyName("status_titulo")]
        public string? StatusTitulo { get; set; }

        [JsonPropertyName("info")]
        public OmieContaPagarInfo? Info { get; set; }
    }

    public class OmieContaPagarInfo
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

    public class ListarContasPagarResponse : OmieResponse<OmieContaPagar>
    {
        [JsonPropertyName("conta_pagar_cadastro")]
        public List<OmieContaPagar> ContasPagar { get; set; } = new();
    }
}
