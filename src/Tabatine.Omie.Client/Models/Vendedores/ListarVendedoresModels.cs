using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.Vendedores
{
    public class ListarVendedoresParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";

        [JsonPropertyName("filtrar_apenas_alteracao")]
        public string? FiltrarApenasAlteracao { get; set; }

        [JsonPropertyName("filtrar_por_data_de")]
        public string? FiltrarPorDataDe { get; set; }

        [JsonPropertyName("filtrar_por_data_ate")]
        public string? FiltrarPorDataAte { get; set; }
    }

    public class OmieVendedor : IOmieMetadata
    {
        [JsonPropertyName("codigo")]
        public long Codigo { get; set; }

        [JsonPropertyName("dAlt")]
        public string? DAlt { get; set; }

        [JsonPropertyName("hAlt")]
        public string? HAlt { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("inativo")]
        public string Inativo { get; set; } = "N";

        [JsonPropertyName("comissao")]
        public decimal Comissao { get; set; }
    }

    public class ListarVendedoresResponse : OmieResponse<OmieVendedor>
    {
        [JsonPropertyName("cadastro")]
        public List<OmieVendedor> Vendedores { get; set; } = new();
    }
}
