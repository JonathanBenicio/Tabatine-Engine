using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.Geral
{
    public class MeiosPagamentoPesquisarRequest
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }
    }

    public class OmieMeioPagamento
    {
        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;
    }

    public class MeiosPagamentoPesquisarResponse
    {
        [JsonPropertyName("MeiosPagamentoLista")]
        public List<OmieMeioPagamento> MeiosPagamentoLista { get; set; } = new();
    }
}
