using System;

namespace Tabatine.Core.Entities
{
    public class LocalEstoque : OmieEntityBase
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string? Tipo { get; set; }
        public bool Padrao { get; set; }
        public bool Inativo { get; set; }
    }
}
