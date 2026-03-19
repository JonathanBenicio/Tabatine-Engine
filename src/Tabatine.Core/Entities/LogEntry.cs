using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tabatine.Core.Entities
{
    [Table("Logs")]
    public class LogEntry
    {
        public int Id { get; set; }

        [Column("message")]
        public string? Message { get; set; }

        [Column("message_template")]
        public string? MessageTemplate { get; set; }

        [Column("level")]
        public string? Level { get; set; }

        [Column("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [Column("exception")]
        public string? Exception { get; set; }

        [Column("properties", TypeName = "jsonb")]
        public string? Properties { get; set; }

        [Column("log_event", TypeName = "jsonb")]
        public string? LogEvent { get; set; }
    }
}
