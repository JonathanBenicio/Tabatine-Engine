using System;

namespace Tabatine.Core.Entities
{
    public class ContaCorrente : OmieEntityBase
    {
        public string Descricao { get; set; } = string.Empty;
        public string? CodigoIntegracao { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public bool Inativa { get; set; }

        // Relacionamento com Banco
        public Guid? BancoId { get; set; }
        public Banco? Banco { get; set; }
    }
}
