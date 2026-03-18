using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.Geral
{
    public class MeiosPagamentoListarRequest
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";
    }

    public class OmieMeioPagamento
    {
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;
    }

    public class MeiosPagamentoListarResponse
    {
        [JsonPropertyName("MeiosPagamentoLista")]
        public List<OmieMeioPagamento> MeiosPagamentoLista { get; set; } = new();
    }
}
