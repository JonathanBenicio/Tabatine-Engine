using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Estoque;

public record PosicaoEstoqueRequest
{
    [JsonPropertyName("codigo_local_estoque")]
    public long CodigoLocalEstoque { get; init; }

    [JsonPropertyName("id_prod")]
    public long IdProd { get; init; }

    [JsonPropertyName("cod_int")]
    public string? CodInt { get; init; }

    [JsonPropertyName("data")]
    public string? Data { get; init; } // Format DD/MM/YYYY

    [JsonPropertyName("apenas_saldo")]
    public string? ApenasSaldo { get; init; } // "S" or "N"
}

public record PosicaoEstoqueResponse
{
    [JsonPropertyName("codigo_status")]
    public string? CodigoStatus { get; init; }

    [JsonPropertyName("descricao_status")]
    public string? DescricaoStatus { get; init; }

    [JsonPropertyName("saldo")]
    public decimal Saldo { get; init; }

    [JsonPropertyName("cmc")]
    public decimal Cmc { get; init; }

    [JsonPropertyName("pendente")]
    public decimal Pendente { get; init; }

    [JsonPropertyName("estoque_minimo")]
    public decimal EstoqueMinimo { get; init; }

    [JsonPropertyName("codigo_local_estoque")]
    public long CodigoLocalEstoque { get; init; }

    [JsonPropertyName("reservado")]
    public decimal Reservado { get; init; }

    [JsonPropertyName("fisico")]
    public decimal Fisico { get; init; }
}

public record ListarPosEstoqueRequest
{
    [JsonPropertyName("nPagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("nRegPorPagina")]
    public int RegPorPagina { get; init; } = 100;

    [JsonPropertyName("dDataPosicao")]
    public string? DataPosicao { get; init; } // Format DD/MM/YYYY

    [JsonPropertyName("cExibeTodos")]
    public string? ExibeTodos { get; init; }

    [JsonPropertyName("cExibeApenasPDV")]
    public string? ExibeApenasPDV { get; init; }

    [JsonPropertyName("codigo_local_estoque")]
    public long? CodigoLocalEstoque { get; init; }
}

public record ListarPosEstoqueResponse
{
    [JsonPropertyName("nPagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("nTotPaginas")]
    public int TotPaginas { get; init; }

    [JsonPropertyName("nRegistros")]
    public int Registros { get; init; }

    [JsonPropertyName("nTotRegistros")]
    public int TotRegistros { get; init; }

    [JsonPropertyName("produtos")]
    public List<ProdutoEstoqueDto> Produtos { get; init; } = new();
}

public record ProdutoEstoqueDto
{
    [JsonPropertyName("nCodProd")]
    public long CodProd { get; init; }

    [JsonPropertyName("cCodInt")]
    public string? CodInt { get; init; }

    [JsonPropertyName("cCodigo")]
    public string? Codigo { get; init; }

    [JsonPropertyName("cDescricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("nPrecoUnitario")]
    public decimal PrecoUnitario { get; init; }

    [JsonPropertyName("nSaldo")]
    public decimal Saldo { get; init; }

    [JsonPropertyName("nCMC")]
    public decimal Cmc { get; init; }

    [JsonPropertyName("nPendente")]
    public decimal Pendente { get; init; }

    [JsonPropertyName("estoque_minimo")]
    public decimal EstoqueMinimo { get; init; }

    [JsonPropertyName("codigo_local_estoque")]
    public long CodigoLocalEstoque { get; init; }

    [JsonPropertyName("reservado")]
    public decimal Reservado { get; init; }

    [JsonPropertyName("fisico")]
    public decimal Fisico { get; init; }
}

public record ListarMovimentoEstoqueRequest
{
    [JsonPropertyName("nPagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("nRegPorPagina")]
    public int RegPorPagina { get; init; } = 100;

    [JsonPropertyName("idProd")]
    public long? IdProd { get; init; }

    [JsonPropertyName("dDtInicial")]
    public string? DtInicial { get; init; } // Format DD/MM/YYYY

    [JsonPropertyName("dDtFinal")]
    public string? DtFinal { get; init; } // Format DD/MM/YYYY

    [JsonPropertyName("codigo_local_estoque")]
    public long? CodigoLocalEstoque { get; init; }
}

public record ListarMovimentoEstoqueResponse
{
    [JsonPropertyName("nPagina")]
    public int Pagina { get; init; }

    [JsonPropertyName("nTotPaginas")]
    public int TotPaginas { get; init; }

    [JsonPropertyName("movProdutoListar")]
    public List<MovimentoEstoqueDto> Movimentos { get; init; } = new();
}

public record MovimentoEstoqueDto
{
    [JsonPropertyName("idMov")]
    public long IdMov { get; init; }

    [JsonPropertyName("dtMov")]
    public string? DtMov { get; init; }

    [JsonPropertyName("dtEmissao")]
    public string? DtEmissao { get; init; }

    [JsonPropertyName("codOrigem")]
    public string? CodOrigem { get; init; }

    [JsonPropertyName("desOrigem")]
    public string? DesOrigem { get; init; }

    [JsonPropertyName("numDoc")]
    public string? NumDoc { get; init; }

    [JsonPropertyName("numPedido")]
    public string? NumPedido { get; init; }

    [JsonPropertyName("operacao")]
    public string? Operacao { get; init; }

    [JsonPropertyName("codigo_local_estoque")]
    public long CodigoLocalEstoque { get; init; }

    [JsonPropertyName("idProd")]
    public long IdProd { get; init; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; init; }

    [JsonPropertyName("descricao")]
    public string? Descricao { get; init; }

    [JsonPropertyName("cmc")]
    public decimal Cmc { get; init; }

    [JsonPropertyName("qtde")]
    public decimal Qtde { get; init; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; init; }

    [JsonPropertyName("saldo")]
    public decimal Saldo { get; init; }
}
