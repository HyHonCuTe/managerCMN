namespace managerCMN.Models.ViewModels;

public class ScheduledAnnouncementAudienceViewModel
{
    public int AnnouncementId { get; set; }

    public int RecipientCount { get; set; }

    public string TargetSummary { get; set; } = string.Empty;

    public List<string> RecipientNames { get; set; } = new();
}
