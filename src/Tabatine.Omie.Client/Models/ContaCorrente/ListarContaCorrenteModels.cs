using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.ContaCorrente
{
    public class ListarContaCorrenteParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";
    }

    public class OmieContaCorrente
    {
        [JsonPropertyName("nCodCC")]
        public long Codigo { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("cCodCCInt")]
        public string? CodigoIntegracao { get; set; }

        [JsonPropertyName("inativo")]
        public string Inativo { get; set; } = "N";

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonPropertyName("codigo_banco")]
        public string? CodigoBanco { get; set; }
    }

    public class ListarContaCorrenteResponse : OmieResponse<OmieContaCorrente>
    {
        [JsonPropertyName("ListarContasCorrentes")]
        public List<OmieContaCorrente> ContasCorrentes { get; set; } = new();
    }
}
