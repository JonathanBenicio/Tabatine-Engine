using System;

namespace Tabatine.Core.Entities
{
    public abstract class OmieEntityBase
    {
        // ID Interno do nosso banco (UUID no PostgreSQL)
        public Guid Id { get; set; }
        
        // ID Original da Omie (essencial para Upsert/Sincronização)
        public long OmieId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Data da última alteração originada *no sistema Omie*
        public DateTime? OmieUpdatedAt { get; set; }
    }
}
