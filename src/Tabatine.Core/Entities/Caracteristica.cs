using System;
using System.Collections.Generic;

namespace Tabatine.Core.Entities
{
    public class Caracteristica : OmieEntityBase
    {
        public string Nome { get; set; } = string.Empty;
        public ICollection<CaracteristicaValor> Valores { get; set; } = new List<CaracteristicaValor>();
    }

    public class CaracteristicaValor
    {
        public Guid Id { get; set; }
        public Guid CaracteristicaId { get; set; }
        public Caracteristica Caracteristica { get; set; } = null!;
        public string Valor { get; set; } = string.Empty;
        public long? OmieIdConteudo { get; set; }
    }
}
