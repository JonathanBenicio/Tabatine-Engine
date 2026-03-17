using System;

namespace Tabatine.Core.Entities
{
    public class Vendedor : OmieEntityBase
    {
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public decimal Comissao { get; set; }
        public bool Inativo { get; set; }
    }
}
