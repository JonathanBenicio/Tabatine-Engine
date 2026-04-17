using Tabatine.Core.Entities;

namespace Tabatine.Core.Interfaces;

/// <summary>
/// Interface para construir mensagens formatadas para notificações (Telegram, etc).
/// </summary>
public interface INotificationTemplateBuilder
{
    /// <summary>
    /// Constrói uma mensagem rica para uma Nota Fiscal.
    /// </summary>
    string BuildNotaFiscalMessage(NotaFiscal nf, string eventName);
}
