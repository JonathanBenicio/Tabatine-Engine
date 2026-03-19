using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class Cliente : OmieEntityBase
    {
        public string RazaoSocial { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string CnpjCpf { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefone { get; set; }
        public string? InscricaoEstadual { get; set; }
        public string? InscricaoMunicipal { get; set; }
        public bool OptanteSimplesNacional { get; set; }
        
        // Endereço Completo
        public string? Cep { get; set; }
        public string? Estado { get; set; }
        public string? Cidade { get; set; }
        public string? Endereco { get; set; }
        public string? EnderecoNumero { get; set; }
        public string? EnderecoComplemento { get; set; }
        public string? Bairro { get; set; }
        
        // Navegação
        public ICollection<PedidoVenda> Pedidos { get; set; } = new List<PedidoVenda>();
        public ICollection<NotaFiscal> NotasFiscais { get; set; } = new List<NotaFiscal>();
    }
}
