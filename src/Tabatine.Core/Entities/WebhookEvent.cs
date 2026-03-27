using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tabatine.Core.Entities
{
    public class WebhookEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        public string AppKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Event { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "jsonb")]
        public string Payload { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}
