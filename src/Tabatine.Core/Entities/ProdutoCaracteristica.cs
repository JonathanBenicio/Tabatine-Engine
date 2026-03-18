using System;

namespace Tabatine.Core.Entities
{
    public class ProdutoCaracteristica
    {
        public Guid Id { get; set; }

        public Guid ProdutoId { get; set; }
        public Produto Produto { get; set; } = null!;

        public Guid CaracteristicaId { get; set; }
        public Caracteristica Caracteristica { get; set; } = null!;

        public Guid CaracteristicaValorId { get; set; }
        public CaracteristicaValor CaracteristicaValor { get; set; } = null!;
    }
}
