using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.EtapaFaturamento
{
    public class ListarEtapasFaturamentoParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;
    }

    public class OmieEtapaFaturamento
    {
        [JsonPropertyName("cCodigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("cDescricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("cDescrPadrao")]
        public string? DescricaoPadrao { get; set; }

        [JsonPropertyName("cInativo")]
        public string Inativo { get; set; } = "N";
    }

    public class OmieOperacaoEtapa
    {
        [JsonPropertyName("cCodOperacao")]
        public string CodigoOperacao { get; set; } = string.Empty;

        [JsonPropertyName("cDescOperacao")]
        public string DescricaoOperacao { get; set; } = string.Empty;

        [JsonPropertyName("etapas")]
        public List<OmieEtapaFaturamento> Etapas { get; set; } = new();
    }

    public class ListarEtapasFaturamentoResponse : OmieResponse<OmieOperacaoEtapa>
    {
        [JsonPropertyName("cadastros")]
        public List<OmieOperacaoEtapa> Cadastros { get; set; } = new();
    }
}
