using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.NotasFiscais
{
    public class ListarNotasFiscaisParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;
        
        [JsonPropertyName("apenas_autorizadas")]
        public string ApenasAutorizadas { get; set; } = "N";
    }

    public class OmieNotaFiscal
    {
        [JsonPropertyName("nIdNF")]
        public long IdNf { get; set; }

        [JsonPropertyName("cNumero")]
        public string Numero { get; set; } = string.Empty;

        [JsonPropertyName("cChaveNFe")]
        public string ChaveNfe { get; set; } = string.Empty;

        [JsonPropertyName("cStatus")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("dDtEmissao")]
        public string DataEmissao { get; set; } = string.Empty;

        [JsonPropertyName("nValorNF")]
        public decimal ValorTotal { get; set; }

        [JsonPropertyName("nIdPedido")]
        public long? IdPedido { get; set; }

        [JsonPropertyName("nIdCliente")]
        public long IdCliente { get; set; }
    }

    public class ListarNotasFiscaisResponse : OmieResponse<OmieNotaFiscal>
    {
        [JsonPropertyName("nfCadastro")]
        public List<OmieNotaFiscal> NotasFiscais { get; set; } = new();
    }
}
