using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Pedidos;

public record StatusPedidoResponse
{
    [JsonPropertyName("codigo_pedido")]
    public long CodigoPedido { get; init; }

    [JsonPropertyName("numero_pedido")]
    public string NumeroPedido { get; init; } = string.Empty;

    [JsonPropertyName("faturada")]
    public string Faturada { get; init; } = string.Empty;

    [JsonPropertyName("cancelada")]
    public string Cancelada { get; init; } = string.Empty;

    [JsonPropertyName("ListaNfe")]
    public List<StatusPedidoNfe> ListaNfe { get; init; } = [];
}

public record StatusPedidoNfe
{
    [JsonPropertyName("status_nfe")]
    public string StatusNfe { get; init; } = string.Empty;

    [JsonPropertyName("numero_nfe")]
    public string NumeroNfe { get; init; } = string.Empty;

    [JsonPropertyName("chave_nfe")]
    public string ChaveNfe { get; init; } = string.Empty;

    [JsonPropertyName("danfe")]
    public string Danfe { get; init; } = string.Empty;
}
