using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class TelegramLinkViewModel
{
    public bool AlreadyLinked { get; set; }
    public bool BotConfigured { get; set; }
    public string? Token { get; set; }
    public string? DeepLink { get; set; }
    public string? BotUsername { get; set; }
    public bool ShowNotificationPreferences { get; set; }
    public List<TelegramNotificationOptionViewModel> NotificationOptions { get; set; } = [];
}

public class TelegramNotificationOptionViewModel
{
    public TelegramNotificationCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public bool IsEnabled { get; set; }
}
