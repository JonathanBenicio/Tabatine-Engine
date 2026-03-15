using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Produtos
{
    public class ListarProdutosParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";
    }

    public class OmieProduto
    {
        [JsonPropertyName("codigo_produto")]
        public long CodigoProduto { get; set; }

        [JsonPropertyName("codigo")]
        public string Codigo { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("valor_unitario")]
        public decimal ValorUnitario { get; set; }

        [JsonPropertyName("ncm")]
        public string Ncm { get; set; } = string.Empty;
        
        [JsonPropertyName("inativo")]
        public string Inativo { get; set; } = "N";
    }

    public class ListarProdutosResponse : OmieResponse<OmieProduto>
    {
        [JsonPropertyName("produto_servico_cadastro")]
        public List<OmieProduto> ProdutosCadastro { get; set; } = new();
    }
}
