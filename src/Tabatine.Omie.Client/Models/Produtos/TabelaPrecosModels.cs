using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Tabatine.Omie.Client.Models.Produtos
{
    public class TabelaPrecosListarRequest
    {
        [JsonPropertyName("nPagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("nRegPorPagina")]
        public int RegistrosPorPagina { get; set; } = 100;
    }

    public class OmieTabelaPreco
    {
        [JsonPropertyName("nCodTabPreco")]
        public long CodigoTabelaPreco { get; set; }

        [JsonPropertyName("cNome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("cCodigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("cAtiva")]
        public string Ativa { get; set; } = "S";
    }

    public class TabelaPrecosListarResponse
    {
        [JsonPropertyName("nPagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("nTotPaginas")]
        public int TotalDePaginas { get; set; }

        [JsonPropertyName("listaTabelasPreco")]
        public List<OmieTabelaPreco> ListaTabelasPreco { get; set; } = new();
    }

    public class TabelaItensListarRequest
    {
        [JsonPropertyName("nPagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("nRegPorPagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("nCodTabPreco")]
        public long CodigoTabelaPreco { get; set; }
    }

    public class OmieTabelaPrecoItem
    {
        [JsonPropertyName("nCodProd")]
        public long CodigoProduto { get; set; }

        [JsonPropertyName("nValorTabela")]
        public decimal ValorTabela { get; set; }
    }

    public class TabelaItensListarResponse
    {
        [JsonPropertyName("nPagina")]
        public int Pagina { get; set; }

        [JsonPropertyName("nTotPaginas")]
        public int TotalDePaginas { get; set; }

        [JsonPropertyName("listaTabelaPreco")]
        public List<TabelaPrecoItensWrapper> ListaTabelaPreco { get; set; } = new();
    }

    public class TabelaPrecoItensWrapper
    {
        [JsonPropertyName("itensTabela")]
        public List<OmieTabelaPrecoItem> ItensTabela { get; set; } = new();
    }
}
