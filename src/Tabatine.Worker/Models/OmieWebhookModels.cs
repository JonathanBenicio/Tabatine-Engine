using System.Text.Json.Serialization;

namespace Tabatine.Worker.Models
{
    public class OmieWebhookRequest
    {
        [JsonPropertyName("appKey")]
        public string? AppKey { get; set; }

        [JsonPropertyName("appHash")]
        public string? AppHash { get; set; }

        [JsonPropertyName("event")]
        public string? Event { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("message")]
        public dynamic? Message { get; set; }
    }

    public class OmieWebhookPedidoMessage
    {
        [JsonPropertyName("codigo_pedido_omie")]
        public long CodigoPedido { get; set; }

        [JsonPropertyName("numero_pedido")]
        public string? NumeroPedido { get; set; }

        [JsonPropertyName("codigo_cliente_omie")]
        public long CodigoCliente { get; set; }

        [JsonPropertyName("etapa")]
        public string? Etapa { get; set; }
        
        [JsonPropertyName("status_pedido")]
        public string? Status { get; set; }
    }

    public class OmieWebhookNfMessage
    {
        [JsonPropertyName("codigo_nf")]
        public long CodigoNf { get; set; }

        [JsonPropertyName("numero_nf")]
        public string? NumeroNf { get; set; }

        [JsonPropertyName("codigo_pedido_omie")]
        public long CodigoPedido { get; set; }
    }
}
