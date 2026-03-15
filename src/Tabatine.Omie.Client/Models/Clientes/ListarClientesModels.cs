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
        
        [JsonPropertyName("exibir_caracteristicas")]
        public string ExibirCaracteristicas { get; set; } = "N";
    }

    public class OmieCliente
    {
        [JsonPropertyName("codigo_cliente_omie")]
        public long CodigoClienteOmie { get; set; }

        [JsonPropertyName("razao_social")]
        public string RazaoSocial { get; set; } = string.Empty;

        [JsonPropertyName("nome_fantasia")]
        public string NomeFantasia { get; set; } = string.Empty;

        [JsonPropertyName("cnpj_cpf")]
        public string CnpjCpf { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    public class ListarClientesResponse : OmieResponse<OmieCliente>
    {
        [JsonPropertyName("clientes_cadastro")]
        public List<OmieCliente> ClientesCadastro { get; set; } = new();
    }
}
