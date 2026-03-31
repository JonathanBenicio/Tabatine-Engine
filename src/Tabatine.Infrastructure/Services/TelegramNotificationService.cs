using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Interfaces;
using Tabatine.Infrastructure.Data;

namespace Tabatine.Infrastructure.Services;

public class TelegramNotificationService(
    HttpClient httpClient,
    IConfiguration configuration,
    AppDbContext dbContext,
    ILogger<TelegramNotificationService> logger) : INotificationService
{
    // ── Global broadcast (existing behaviour) ─────────────────────────────────

    public async Task SendNotificationAsync(string title, string message, string type, long? referenceId = null)
    {
        var botToken = configuration["Telegram:BotToken"];

        if (string.IsNullOrEmpty(botToken))
        {
            logger.LogWarning("Telegram BotToken não configurado. Notificação não enviada.");
            return;
        }

        // Busca todos os perfis que devem receber logs e possuem um ChatId vinculado
        var logRecipients = await dbContext.Perfis
            .Where(p => p.ReceiveLogs && p.TelegramChatId.HasValue)
            .Select(p => p.TelegramChatId!.Value)
            .ToListAsync();

        if (logRecipients.Count == 0)
        {
            logger.LogInformation("Nenhum destinatário de log do Telegram configurado no banco de dados.");
            return;
        }

        var text = $"*{title}*\n\n{message}\n\nTipo: {type}\nID Omie: {referenceId}";
        
        foreach (var chatId in logRecipients)
        {
            await SendRawMessageAsync(chatId.ToString(), text);
        }
    }

    // ── Direct message to a specific chat (Deep Link) ─────────────────────────

    public async Task SendDirectMessageAsync(long chatId, string message)
    {
        var botToken = configuration["Telegram:BotToken"];

        if (string.IsNullOrEmpty(botToken))
        {
            logger.LogWarning("Telegram BotToken não configurado. Mensagem direta não enviada.");
            return;
        }

        await SendRawMessageAsync(chatId.ToString(), message);
    }

    // ── Internal helper ───────────────────────────────────────────────────────

    private async Task SendRawMessageAsync(string chatId, string text)
    {
        try
        {
            var botToken = configuration["Telegram:BotToken"];
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage" +
                      $"?chat_id={chatId}" +
                      $"&text={Uri.EscapeDataString(text)}" +
                      $"&parse_mode=Markdown";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                logger.LogError("Erro ao enviar mensagem para o Telegram. ChatId={ChatId}, Error={Error}", chatId, error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na comunicação com a Telegram API. ChatId={ChatId}", chatId);
        }
    }
}
