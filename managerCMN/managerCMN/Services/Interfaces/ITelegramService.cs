namespace managerCMN.Services.Interfaces;

public interface ITelegramService
{
    bool IsConfigured { get; }
    string? BotUsername { get; }
    Task SendMessageAsync(string chatId, string text);
    string GetLinkDeepLink(string token);
}
