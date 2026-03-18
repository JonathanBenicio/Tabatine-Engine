using System;

namespace Tabatine.Core.Entities
{
    public class Banco : OmieEntityBase
    {
        public string CodigoBanco { get; set; } = string.Empty; // Ex: "001", "341"
        public string Nome { get; set; } = string.Empty;
        public string? CodigoIspb { get; set; }
        public string? Tipo { get; set; }
    }
}
