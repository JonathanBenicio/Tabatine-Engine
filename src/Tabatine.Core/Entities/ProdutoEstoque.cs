using System;

namespace Tabatine.Core.Entities
{
    public class ProdutoEstoque
    {
        public Guid Id { get; set; }
        
        public Guid ProdutoId { get; set; }
        public virtual Produto Produto { get; set; } = null!;
        
        public Guid LocalEstoqueId { get; set; }
        public virtual LocalEstoque LocalEstoque { get; set; } = null!;
        
        public decimal Saldo { get; set; }
        public decimal Reservado { get; set; }
        public decimal Pendente { get; set; }
        public decimal Fisico { get; set; }
        public decimal Cmc { get; set; }
        public decimal EstoqueMinimo { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}
