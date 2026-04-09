using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Estoque;

public record ListarLocaisEstoqueRequest
{
    [JsonPropertyName("nPagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("nRegPorPagina")]
    public int RegPorPagina { get; init; } = 100;

    [JsonPropertyName("filtrar_por_data_de")]
    public string? FiltrarPorDataDe { get; init; }

    [JsonPropertyName("filtrar_por_data_ate")]
    public string? FiltrarPorDataAte { get; init; }
}

public record ListarLocaisEstoqueResponse
{
    [JsonPropertyName("nPagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("nTotPaginas")]
    public int TotPaginas { get; init; }

    [JsonPropertyName("nRegistros")]
    public int Registros { get; init; }

    [JsonPropertyName("nTotRegistros")]
    public int TotRegistros { get; init; }

    [JsonPropertyName("locaisEncontrados")]
    public List<LocalEstoqueDto> Locais { get; init; } = new();
}

public record LocalEstoqueDto
{
    [JsonPropertyName("codigo_local_estoque")]
    public long CodigoLocalEstoque { get; init; }

    [JsonPropertyName("codigo")]
    public string? Codigo { get; init; }

    [JsonPropertyName("descricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; init; }

    [JsonPropertyName("padrao")]
    public string? Padrao { get; init; } // "S" ou "N"

    [JsonPropertyName("inativo")]
    public string? Inativo { get; init; } // "S" ou "N"
}
