using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tabatine.Infrastructure.Data;
using Tabatine.Infrastructure.Services;

namespace Tabatine.Worker.Endpoints;

public static class TelegramWebhookEndpoints
{
    public static void MapTelegramWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        // Recebe updates do Telegram Bot
        app.MapPost("/api/webhooks/telegram", async (
            HttpContext context,
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TelegramWebhook");

            // 1. Validar Secret Token (Segurança)
            var expectedSecret = configuration["Telegram:WebhookSecret"];
            if (!string.IsNullOrEmpty(expectedSecret))
            {
                var receivedSecret = context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
                if (receivedSecret != expectedSecret)
                {
                    logger.LogWarning("Tentativa de acesso ao Webhook com Secret Token inválido ou ausente.");
                    return Results.Unauthorized();
                }
            }

            try
            {
                using var document = await JsonDocument.ParseAsync(context.Request.Body);
                var root = document.RootElement;

                // O Telegram envia { "message": { "text": "...", "chat": { "id": ... } } }
                if (!root.TryGetProperty("message", out var messageElement))
                {
                    return Results.Ok();
                }

                if (!messageElement.TryGetProperty("text", out var textElement) ||
                    !messageElement.TryGetProperty("chat", out var chatElement) ||
                    !chatElement.TryGetProperty("id", out var chatIdElement))
                {
                    return Results.Ok();
                }

                var text = textElement.GetString() ?? string.Empty;
                var chatId = chatIdElement.GetInt64();

                logger.LogInformation("Update Telegram recebido. ChatId={ChatId}, Text={Text}", chatId, text);

                // Detecta o comando /start emitido pelo Deep Link
                if (!text.StartsWith("/start "))
                    return Results.Ok();

                var tokenString = text["/start ".Length..].Trim();

                if (!Guid.TryParse(tokenString, out var tokenGuid))
                {
                    logger.LogWarning("Token de vinculação Telegram inválido (não é um UUID). Token={Token}", tokenString);
                    await SendReply(serviceProvider, chatId, "❌ Link de vinculação inválido. Gere um novo no painel Tabatine.");
                    return Results.Ok();
                }

                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // 2. Buscar Perfil pelo Token
                var perfil = await dbContext.Perfis
                    .OrderBy(p => p.Id)
                    .FirstOrDefaultAsync(p => p.TelegramLinkToken == tokenGuid);

                if (perfil is null)
                {
                    logger.LogWarning("Nenhum perfil encontrado para o token Telegram. Token={Token}", tokenGuid);
                    await SendReply(serviceProvider, chatId, "❌ Link inválido ou já utilizado. Gere um novo no painel Tabatine.");
                    return Results.Ok();
                }

                // 3. Verificar Expiração (15 minutos)
                if (perfil.TelegramLinkTokenExpiresAt.HasValue && perfil.TelegramLinkTokenExpiresAt < DateTime.UtcNow)
                {
                    logger.LogWarning("Token de vinculação expirado. Token={Token}, ExpirouEm={ExpiresAt}", tokenGuid, perfil.TelegramLinkTokenExpiresAt);
                    await SendReply(serviceProvider, chatId, "❌ Este link expirou (validade de 15 min). Gere um novo no painel Tabatine.");
                    return Results.Ok();
                }

                // 4. Vincular e invalidar o token (uso único)
                perfil.TelegramChatId = chatId;
                perfil.TelegramLinkToken = null;
                perfil.TelegramLinkTokenExpiresAt = null;
                perfil.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();

                logger.LogInformation(
                    "Telegram vinculado com sucesso ao Perfil={Id}, ChatId={ChatId}",
                    perfil.Id, chatId);

                await SendReply(serviceProvider, chatId,
                    $"✅ Conta vinculada com sucesso!\n\nA partir de agora você receberá notificações importantes do sistema aqui.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao processar update do Telegram.");
            }

            // Telegram exige sempre HTTP 200 para não reenviar
            return Results.Ok();
        });

        // Endpoint auxiliar para registar o Webhook no Telegram
        app.MapGet("/api/telegram/setup-webhook", async (
            IConfiguration configuration,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("TelegramSetup");
            var botToken = configuration["Telegram:BotToken"];
            var webhookUrl = configuration["Telegram:WebhookUrl"];
            var secretToken = configuration["Telegram:WebhookSecret"];

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(webhookUrl))
            {
                logger.LogWarning("Telegram:BotToken ou Telegram:WebhookUrl não configurados.");
                return Results.BadRequest("Configure Telegram:BotToken e Telegram:WebhookUrl nas variáveis de ambiente.");
            }

            using var http = new HttpClient();
            var setupUrl = $"https://api.telegram.org/bot{botToken}/setWebhook?url={Uri.EscapeDataString(webhookUrl + "/api/webhooks/telegram")}";
            
            if (!string.IsNullOrEmpty(secretToken))
            {
                setupUrl += $"&secret_token={Uri.EscapeDataString(secretToken)}";
            }

            var response = await http.GetAsync(setupUrl);
            var body = await response.Content.ReadAsStringAsync();

            logger.LogInformation("Telegram setWebhook response: {Body}", body);
            return Results.Content(body, "application/json");
        });
    }

    private static async Task SendReply(IServiceProvider serviceProvider, long chatId, string message)
    {
        using var scope = serviceProvider.CreateScope();
        var telegram = scope.ServiceProvider.GetRequiredService<TelegramNotificationService>();
        await telegram.SendDirectMessageAsync(chatId, message);
    }
}
