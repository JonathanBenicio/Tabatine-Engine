using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.FormaPagamento
{
    public class ListarFormasPagVendasParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;
    }

    public class OmieFormaPagamento
    {
        [JsonPropertyName("cCodigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("cDescricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("nQtdeParc")]
        public int QuantidadeParcelas { get; set; }

        [JsonPropertyName("nDiasParc")]
        public int? DiasParcelas { get; set; }

        [JsonPropertyName("cListaParc")]
        public string? ListaParcelas { get; set; }
    }

    public class ListarFormasPagVendasResponse : OmieResponse<OmieFormaPagamento>
    {
        [JsonPropertyName("cadastros")]
        public List<OmieFormaPagamento> FormasPagamento { get; set; } = new();
    }
}
