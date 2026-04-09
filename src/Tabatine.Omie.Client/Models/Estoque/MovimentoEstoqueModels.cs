using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Estoque;

public record ListarMovimentosRequest
{
    [JsonPropertyName("pagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("registros_por_pagina")]
    public int RegistrosPorPagina { get; init; } = 100;

    [JsonPropertyName("data_inicial")]
    public string? DataInicial { get; init; } // Format DD/MM/YYYY

    [JsonPropertyName("data_final")]
    public string? DataFinal { get; init; } // Format DD/MM/YYYY

    [JsonPropertyName("codigo_local_estoque")]
    public long? CodigoLocalEstoque { get; init; }
}

public record ListarMovimentosResponse
{
    [JsonPropertyName("pagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("total_de_paginas")]
    public int TotalPaginas { get; init; }

    [JsonPropertyName("registros")]
    public int Registros { get; init; }

    [JsonPropertyName("total_de_registros")]
    public int TotalRegistros { get; init; }

    [JsonPropertyName("cadastros")]
    public List<MovimentoProdutoDto> Cadastros { get; init; } = new();
}

public record MovimentoProdutoDto
{
    [JsonPropertyName("nCodProd")]
    public long CodProd { get; init; }

    [JsonPropertyName("cCodIntProd")]
    public string? CodIntProd { get; init; }

    [JsonPropertyName("cCodigo")]
    public string? Codigo { get; init; }

    [JsonPropertyName("cDescricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("movimentos")]
    public List<MovimentoDetalheDto> Movimentos { get; init; } = new();
}

public record MovimentoDetalheDto
{
    [JsonPropertyName("dDataMovimento")]
    public string? DataMovimento { get; init; }

    [JsonPropertyName("nQtdeEntradas")]
    public decimal QtdeEntradas { get; init; }

    [JsonPropertyName("nQtdeSaidas")]
    public decimal QtdeSaidas { get; init; }
}
