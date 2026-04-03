namespace managerCMN.Models.ViewModels;

public class BirthdayCelebrationViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string BirthdayDateLabel { get; set; } = string.Empty;
    public string CountdownTargetIso { get; set; } = string.Empty;
    public int DaysUntilBirthday { get; set; }
    public int UpcomingAge { get; set; }
    public bool IsBirthdayToday { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CountdownLabel { get; set; } = string.Empty;
    public string AddressPronoun { get; set; } = "bạn";
    public string TodaySpecialWish { get; set; } = string.Empty;
}
