using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class TabelaPreco : OmieEntityBase
    {
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public ICollection<TabelaPrecoItem> Itens { get; set; } = new List<TabelaPrecoItem>();
    }

    public class TabelaPrecoItem
    {
        public Guid Id { get; set; }
        public Guid TabelaPrecoId { get; set; }
        public TabelaPreco TabelaPreco { get; set; } = null!;
        public Guid ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;
        public decimal Valor { get; set; }
    }
}
