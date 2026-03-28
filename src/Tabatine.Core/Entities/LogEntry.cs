using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tabatine.Core.Entities
{
    public class LogEntry
    {
        public int Id { get; set; }

        public string? Message { get; set; }

        public string? MessageTemplate { get; set; }

        public string? Level { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string? Exception { get; set; }

        public string? Properties { get; set; }

        public string? LogEvent { get; set; }
    }
}
