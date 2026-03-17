using System;

namespace Tabatine.Core.Entities
{
    public class IntegrationSyncState
    {
        public Guid Id { get; set; }
        public string ModuleName { get; set; } = string.Empty;  // "Clientes", "Produtos", etc.
        public DateTime LastSyncDate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
