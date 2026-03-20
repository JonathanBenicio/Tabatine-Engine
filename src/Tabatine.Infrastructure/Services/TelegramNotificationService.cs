using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tabatine.Core.Interfaces;

namespace Tabatine.Infrastructure.Services
{
    public class TelegramNotificationService : INotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TelegramNotificationService> _logger;

        public TelegramNotificationService(HttpClient httpClient, IConfiguration configuration, ILogger<TelegramNotificationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string title, string message, string type, long? referenceId = null)
        {
            var botToken = _configuration["Telegram:BotToken"];
            var chatId = _configuration["Telegram:ChatId"];

            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("Telegram BotToken ou ChatId não configurados. Notificação não enviada.");
                return;
            }

            var text = $"* {title} *\n\n{message}\n\nTipo: {type}\nID Omie: {referenceId}";
            
            try
            {
                var response = await _httpClient.GetAsync($"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(text)}&parse_mode=Markdown");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erro ao enviar notificação para o Telegram: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na comunicação com o Telegram API.");
            }
        }
    }
}
