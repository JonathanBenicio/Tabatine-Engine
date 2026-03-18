using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.Geral
{
    public class CaracteristicasListarRequest
    {
        [JsonPropertyName("nPagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("nRegPorPagina")]
        public int RegistrosPorPagina { get; set; } = 100;
    }

    public class OmieCaracteristica
    {
        [JsonPropertyName("nCodCaract")]
        public long CodigoCaracteristica { get; set; }

        [JsonPropertyName("cNomeCaract")]
        public string NomeCaracteristica { get; set; } = string.Empty;

        [JsonPropertyName("conteudosPermitidos")]
        public List<OmieCaracteristicaConteudo>? ConteudosPermitidos { get; set; }
    }

    public class OmieCaracteristicaConteudo
    {
        [JsonPropertyName("cConteudo")]
        public string Conteudo { get; set; } = string.Empty;

        [JsonPropertyName("nIdConteudo")]
        public long IdConteudo { get; set; }
    }

    public class CaracteristicasListarResponse
    {
        [JsonPropertyName("nPagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("nTotPaginas")]
        public int TotalDePaginas { get; set; }

        [JsonPropertyName("nRegistros")]
        public int Registros { get; set; }

        [JsonPropertyName("nTotRegistros")]
        public int TotalDeRegistros { get; set; }

        [JsonPropertyName("listaCaracteristicas")]
        public List<OmieCaracteristica> ListaCaracteristicas { get; set; } = new();
    }
}
