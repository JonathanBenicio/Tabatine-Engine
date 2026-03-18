using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.Bancos
{
    public class ListarBancosParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;
    }

    public class OmieBanco
    {
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("tipo")]
        public string? Tipo { get; set; }

        [JsonPropertyName("cod_ispb")]
        public string? CodigoIspb { get; set; }
    }

    public class ListarBancosResponse : OmieResponse<OmieBanco>
    {
        [JsonPropertyName("fin_banco_cadastro")]
        public List<OmieBanco> Bancos { get; set; } = new();
    }
}
