using System;

namespace Tabatine.Core.Entities;

public class Perfil
{
    public Guid Id { get; set; }
    public string? Nome { get; set; }
    public long? TelegramChatId { get; set; }
    public Guid? TelegramLinkToken { get; set; }
    public DateTime? TelegramLinkTokenExpiresAt { get; set; }
    public bool ReceiveLogs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
