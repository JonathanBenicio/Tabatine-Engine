using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models
{
    public class OmieRequest<T>
    {
        [JsonPropertyName("app_key")]
        public string AppKey { get; set; } = string.Empty;

        [JsonPropertyName("app_secret")]
        public string AppSecret { get; set; } = string.Empty;

        [JsonPropertyName("call")]
        public string Call { get; set; } = string.Empty;

        [JsonPropertyName("param")]
        public List<T> Param { get; set; } = new();
    }

    public class OmieResponse<T>
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("total_de_paginas")]
        public int TotalDePaginas { get; set; }

        [JsonPropertyName("registros")]
        public int Registros { get; set; }

        [JsonPropertyName("total_de_registros")]
        public int TotalDeRegistros { get; set; }
        
        // This will be specialized by child classes or dynamic
    }
}
