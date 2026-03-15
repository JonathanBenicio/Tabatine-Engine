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
        
        // Endereço Básico
        public string? Cep { get; set; }
        public string? Estado { get; set; }
        public string? Cidade { get; set; }
        
        // Navegação
        public ICollection<PedidoVenda> Pedidos { get; set; } = new List<PedidoVenda>();
    }
}
