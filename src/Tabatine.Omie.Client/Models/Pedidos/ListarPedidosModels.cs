using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Pedidos
{
    public class ListarPedidosParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;
        
        [JsonPropertyName("apenas_faturados")]
        public string ApenasFaturados { get; set; } = "N";
    }

    public class OmiePedido
    {
        [JsonPropertyName("cabecalho")]
        public OmiePedidoCabecalho Cabecalho { get; set; } = new();

        [JsonPropertyName("detalhe")]
        public List<OmiePedidoItem> Detalhe { get; set; } = new();
    }

    public class OmiePedidoCabecalho
    {
        [JsonPropertyName("codigo_pedido_omie")]
        public long CodigoPedidoOmie { get; set; }

        [JsonPropertyName("numero_pedido")]
        public string NumeroPedido { get; set; } = string.Empty;

        [JsonPropertyName("codigo_cliente")]
        public long CodigoCliente { get; set; }

        [JsonPropertyName("etapa")]
        public string Etapa { get; set; } = string.Empty;

        [JsonPropertyName("valor_total")]
        public decimal ValorTotal { get; set; }
        
        [JsonPropertyName("data_previsao")]
        public string DataPrevisao { get; set; } = string.Empty;
    }

    public class OmiePedidoItem
    {
        [JsonPropertyName("produto")]
        public OmiePedidoItemProduto Produto { get; set; } = new();
    }

    public class OmiePedidoItemProduto
    {
        [JsonPropertyName("codigo_produto")]
        public long CodigoProduto { get; set; }

        [JsonPropertyName("quantidade")]
        public decimal Quantidade { get; set; }

        [JsonPropertyName("valor_unitario")]
        public decimal ValorUnitario { get; set; }
    }

    public class ListarPedidosResponse : OmieResponse<OmiePedido>
    {
        [JsonPropertyName("pedido_venda_produto")]
        public List<OmiePedido> PedidosVenda { get; set; } = new();
    }
}
