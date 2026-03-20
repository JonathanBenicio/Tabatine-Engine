using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tabatine.Core.Entities
{
    [Table("Notifications")]
    public class Notification
    {
        public Guid Id { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("type")]
        public string Type { get; set; } = string.Empty; // Sale, NF

        [Column("reference_id")]
        public long? ReferenceId { get; set; } // OmieId (ex: CodigoPedido ou CodigoNF)

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
