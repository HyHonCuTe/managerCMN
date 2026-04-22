using System.Text;
using System.Text.Json;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class TelegramService : ITelegramService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _botToken;
    private readonly ILogger<TelegramService> _logger;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_botToken);
    public string? BotUsername { get; }

    public TelegramService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<TelegramService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _botToken = config["Telegram:BotToken"];
        BotUsername = config["Telegram:BotUsername"];
    }

    public async Task SendMessageAsync(string chatId, string text)
    {
        if (!IsConfigured) return;

        try
        {
            var client = _httpClientFactory.CreateClient("Telegram");
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new StringContent(
                JsonSerializer.Serialize(new { chat_id = chatId, text, parse_mode = "HTML" }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Telegram API error for chatId {ChatId}: {Body}", chatId, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram message to chatId {ChatId}", chatId);
        }
    }

    public string GetLinkDeepLink(string token)
        => $"https://t.me/{BotUsername}?start={token}";
}
