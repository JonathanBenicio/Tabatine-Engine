using System.Text.Json.Serialization;

namespace Tabatine.Omie.Client.Models.Clientes
{
    public class ListarClientesParam
    {
        [JsonPropertyName("pagina")]
        public int Pagina { get; set; } = 1;

        [JsonPropertyName("registros_por_pagina")]
        public int RegistrosPorPagina { get; set; } = 100;

        [JsonPropertyName("apenas_importado_api")]
        public string ApenasImportadoApi { get; set; } = "N";

        [JsonPropertyName("filtrar_apenas_alteracao")]
        public string FiltrarApenasAlteracao { get; set; } = "N";

        [JsonPropertyName("filtrar_por_data_de")]
        public string? FiltrarPorDataDe { get; set; }

        [JsonPropertyName("filtrar_por_data_ate")]
        public string? FiltrarPorDataAte { get; set; }

        [JsonPropertyName("filtrar_por_hora_de")]
        public string? FiltrarPorHoraDe { get; set; }

        [JsonPropertyName("filtrar_por_hora_ate")]
        public string? FiltrarPorHoraAte { get; set; }
    }

    public class ClientesFiltro
    {
        // A API Omie não suporta filtro por data no clientesFiltro.
        // Campos válidos: codigo_cliente_omie, cnpj_cpf, razao_social, etc.
    }

    public class OmieCliente : IOmieMetadata
    {
        [JsonPropertyName("codigo_cliente_omie")]
        public long CodigoClienteOmie { get; set; }

        [JsonPropertyName("dAlt")]
        public string? DAlt { get; set; }

        [JsonPropertyName("hAlt")]
        public string? HAlt { get; set; }

        [JsonPropertyName("razao_social")]
        public string RazaoSocial { get; set; } = string.Empty;

        [JsonPropertyName("nome_fantasia")]
        public string NomeFantasia { get; set; } = string.Empty;

        [JsonPropertyName("cnpj_cpf")]
        public string CnpjCpf { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("telefone_numero")]
        public string? Telefone { get; set; }

        [JsonPropertyName("endereco")]
        public string? Endereco { get; set; }

        [JsonPropertyName("endereco_numero")]
        public string? EnderecoNumero { get; set; }

        [JsonPropertyName("complemento")]
        public string? Complemento { get; set; }

        [JsonPropertyName("bairro")]
        public string? Bairro { get; set; }

        [JsonPropertyName("cep")]
        public string? Cep { get; set; }

        [JsonPropertyName("estado")]
        public string? Estado { get; set; }

        [JsonPropertyName("cidade")]
        public string? Cidade { get; set; }

        [JsonPropertyName("inscricao_estadual")]
        public string? InscricaoEstadual { get; set; }

        [JsonPropertyName("inscricao_municipal")]
        public string? InscricaoMunicipal { get; set; }

        [JsonPropertyName("optante_simples_nacional")]
        public string? OptanteSimplesNacional { get; set; }

        [JsonPropertyName("recomendacoes")]
        public OmieClienteRecomendacoes? Recomendacoes { get; set; }
    }

    public class OmieClienteRecomendacoes
    {
        [JsonPropertyName("codigo_vendedor")]
        public long? CodigoVendedor { get; set; }
    }

    public class ListarClientesResponse : OmieResponse<OmieCliente>
    {
        [JsonPropertyName("clientes_cadastro")]
        public List<OmieCliente> ClientesCadastro { get; set; } = new();
    }
}
