using System;

namespace Tabatine.Core.Entities
{
    public class MeioPagamento : OmieEntityBase
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
    }
}
