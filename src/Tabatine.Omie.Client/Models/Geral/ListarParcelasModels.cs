using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Geral
{
    public class ListarParcelasParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";

        [JsonPropertyName("ordenar_por")]
        public string? OrdenarPor { get; set; }

        [JsonPropertyName("ordem_decrescente")]
        public string? OrdemDecrescente { get; set; }
    }

    public class ParcelaOmie
    {
        [JsonPropertyName("nCodigo")]
        [JsonConverter(typeof(OmieFlexibleIntConverter))]
        public int Codigo { get; set; }

        [JsonPropertyName("cDescricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("nParcelas")]
        public int QuantidadeParcelas { get; set; }

        [JsonPropertyName("nDiaFixo")]
        public int DiaFixo { get; set; }
    }

    public class ListarParcelasResponse
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("total_de_paginas")]
        public int TotalDePaginas { get; set; }

        [JsonPropertyName("registros")]
        public int Registros { get; set; }

        [JsonPropertyName("total_de_registros")]
        public int TotalDeRegistros { get; set; }

        [JsonPropertyName("cadastros")]
        public List<ParcelaOmie> Cadastros { get; set; } = new();
    }
}
