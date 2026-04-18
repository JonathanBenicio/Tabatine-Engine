using System.Globalization;
using Tabatine.Core.Entities;
using Tabatine.Core.Interfaces;

namespace Tabatine.Infrastructure.Services;

/// <summary>
/// Constrói mensagens formatadas para notificações de Webhooks.
/// </summary>
public class NotificationTemplateBuilder : INotificationTemplateBuilder
{
    public string BuildNotaFiscalMessage(NotaFiscal nf, string eventName)
    {
        var statusEmoji = nf.Status switch
        {
            "Autorizada" => "✅",
            "Cancelada" => "❌",
            "Denegada" => "🚫",
            _ => "ℹ️"
        };

        var title = eventName switch
        {
            "Faturamento.NotaFiscalEmitida" => "NF Emitida",
            "NFe.NotaAutorizada" => "NF-e Autorizada",
            "NFe.NotaCancelada" => "NF-e Cancelada",
            "NFSe.NotaAutorizada" => "NFS-e Autorizada",
            "NFSe.NotaCancelada" => "NFS-e Cancelada",
            _ => "Nota Fiscal Atualizada"
        };

        var culture = new CultureInfo("pt-BR");
        
        // Formata a mensagem principal com Markdown do Telegram
        var message = $"""
            {statusEmoji} *{title}*
            
            📄 *Número:* {nf.NumeroNf}
            👤 *Cliente:* {nf.Cliente?.RazaoSocial ?? "Não identificado"}
            🧑‍💼 *Vendedor:* {nf.Vendedor?.Nome ?? "Não informado"}
            🗓️ *Emissão:* {nf.DataEmissao:dd/MM/yyyy}
            💰 *Total:* {nf.ValorTotal.ToString("C2", culture)}
            📦 *Status:* {nf.Status}
            """;

        // Adiciona Links
        if (!string.IsNullOrEmpty(nf.LinkDanfe))
        {
            message += $"\n\n🧾 [Visualizar DANFE]({nf.LinkDanfe})";
        }

        if (nf.OmieId > 0)
        {
            // Link profundo baseado no padrão conhecido da Omie
            var deepLink = $"https://app.omie.com.br/detalhes/nfe/?codigo_nf={nf.OmieId}";
            var prefix = string.IsNullOrEmpty(nf.LinkDanfe) ? "\n\n" : "\n";
            message += $"{prefix}🔗 [Abrir no Omie]({deepLink})";
        }

        return message;
    }
}
