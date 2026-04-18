using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Pedidos;

public record StatusPedidoRequest
{
    [JsonPropertyName("codigo_pedido")]
    public long? CodigoPedido { get; init; }

    [JsonPropertyName("codigo_pedido_integracao")]
    public string? CodigoPedidoIntegracao { get; init; }
}
