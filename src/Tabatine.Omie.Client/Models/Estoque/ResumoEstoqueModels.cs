using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Estoque;

public record ObterEstoqueProdutoRequest
{
    [JsonPropertyName("cEAN")]
    public string? EAN { get; init; }

    [JsonPropertyName("nIdProduto")]
    public long? IdProduto { get; init; }

    [JsonPropertyName("cCodigo")]
    public string? Codigo { get; init; }
}

public record ObterEstoqueProdutoResponse
{
    [JsonPropertyName("nIdProduto")]
    public long IdProduto { get; init; }

    [JsonPropertyName("cCodigo")]
    public string? Codigo { get; init; }

    [JsonPropertyName("cDescricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("cEAN")]
    public string? EAN { get; init; }

    [JsonPropertyName("listaEstoque")]
    public List<ResumoEstoqueLocalDto> ListaEstoque { get; init; } = new();

    [JsonPropertyName("listaProduto")]
    public List<ResumoProdutoDto> ListaProduto { get; init; } = new();
}

public record ResumoEstoqueLocalDto
{
    [JsonPropertyName("nIdlocal")]
    public long IdLocal { get; init; }

    [JsonPropertyName("nFisico")]
    public decimal Fisico { get; init; }

    [JsonPropertyName("nReservado")]
    public decimal Reservado { get; init; }

    [JsonPropertyName("nPrevisaoSaida")]
    public decimal PrevisaoSaida { get; init; }

    [JsonPropertyName("nPrevisaoEntrada")]
    public decimal PrevisaoEntrada { get; init; }

    [JsonPropertyName("nDisponivel")]
    public decimal Disponivel { get; init; }

    [JsonPropertyName("nCMC")]
    public decimal Cmc { get; init; }

    [JsonPropertyName("nPrecoUnitario")]
    public decimal PrecoUnitario { get; init; }

    [JsonPropertyName("nPrecoUltComp")]
    public decimal PrecoUltComp { get; init; }

    [JsonPropertyName("dDtUltComp")]
    public string? DtUltComp { get; init; }

    [JsonPropertyName("nEstoqueMinimo")]
    public decimal EstoqueMinimo { get; init; }

    [JsonPropertyName("cDescricaoLocal")]
    public string? DescricaoLocal { get; init; }
}

public record ResumoProdutoDto
{
    [JsonPropertyName("nIdProduto")]
    public long IdProduto { get; init; }

    [JsonPropertyName("cCodigo")]
    public string? Codigo { get; init; }

    [JsonPropertyName("cDescricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("cEAN")]
    public string? EAN { get; init; }

    [JsonPropertyName("cNCM")]
    public string? NCM { get; init; }

    [JsonPropertyName("cUnidade")]
    public string? Unidade { get; init; }
}
