using managerCMN.Models.Enums;

namespace managerCMN.Helpers;

public static class LeaveReasonHelper
{
    public record ReasonInfo(LeaveReason Reason, string DisplayName, bool CountsAsWork, RequestType ForType);

    public static readonly List<ReasonInfo> AllReasons = new()
    {
        // Leave
        new(LeaveReason.PaidLeave, "Nghỉ tính phép", true, RequestType.Leave),
        new(LeaveReason.UnpaidLeave, "Nghỉ không phép", false, RequestType.Leave),
        new(LeaveReason.SickLeaveWithCert, "Nghỉ ốm có giấy", true, RequestType.Leave),
        new(LeaveReason.BereavementLeave, "Nghỉ tang (tối đa 3 ngày)", true, RequestType.Leave),
        new(LeaveReason.MaternityLeave, "Nghỉ thai sản", true, RequestType.Leave),
        new(LeaveReason.MarriageLeave, "Nghỉ kết hôn (tối đa 3 ngày)", true, RequestType.Leave),
        new(LeaveReason.OtherLeave, "Khác", true, RequestType.Leave),

        // CheckInOut
        new(LeaveReason.ForgotCheckInOut, "Quên checkin/out", true, RequestType.CheckInOut),

        // Absence
        new(LeaveReason.CompanyBusiness, "Đi công việc CTY", true, RequestType.Absence),
        new(LeaveReason.PersonalBusiness, "Việc cá nhân", true, RequestType.Absence),

        // WorkFromHome
        new(LeaveReason.WorkFromHome, "Xin làm ở nhà", true, RequestType.WorkFromHome),
    };

    public static IEnumerable<ReasonInfo> GetReasonsForType(RequestType type)
        => AllReasons.Where(r => r.ForType == type);

    public static bool GetCountsAsWork(LeaveReason reason)
        => AllReasons.FirstOrDefault(r => r.Reason == reason)?.CountsAsWork ?? true;

    public static string GetDisplayName(LeaveReason reason)
        => AllReasons.FirstOrDefault(r => r.Reason == reason)?.DisplayName ?? reason.ToString();
}
