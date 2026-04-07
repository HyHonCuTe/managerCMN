using managerCMN.Models.Enums;

namespace managerCMN.Helpers;

public static class LeaveReasonHelper
{
    public record ReasonInfo(
        LeaveReason Reason,
        string DisplayName,
        bool CountsAsWork,
        RequestType ForType,
        bool DeductsLeave = false);

    public static readonly List<ReasonInfo> AllReasons = new()
    {
        // Leave
        new(LeaveReason.PaidLeave, "Nghỉ tính phép", true, RequestType.Leave, true),
        new(LeaveReason.UnpaidLeave, "Nghỉ không phép", false, RequestType.Leave),
        new(LeaveReason.SickLeaveWithCert, "Nghỉ ốm/khám thai có giấy", true, RequestType.Leave),
        new(LeaveReason.BereavementLeave, "Nghỉ tang (tối đa 3 ngày)", true, RequestType.Leave),
        new(LeaveReason.MaternityLeave, "Nghỉ thai sản", true, RequestType.Leave),
        new(LeaveReason.MarriageLeave, "Nghỉ kết hôn (tối đa 3 ngày)", true, RequestType.Leave),

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
        => GetReasonInfo(reason)?.CountsAsWork ?? true;

    public static bool GetDeductsLeave(LeaveReason reason)
    {
        var reasonInfo = GetReasonInfo(reason);
        if (reasonInfo != null)
        {
            return reasonInfo.DeductsLeave;
        }

        return GetCountsAsWork(reason);
    }

    public static string GetStatusText(LeaveReason reason)
    {
        if (!GetCountsAsWork(reason))
        {
            return "Không tính công";
        }

        return GetDeductsLeave(reason)
            ? "Tính công, trừ phép"
            : "Tính công, không trừ phép";
    }

    public static string GetDisplayName(LeaveReason reason)
        => GetReasonInfo(reason)?.DisplayName ?? reason.ToString();

    private static ReasonInfo? GetReasonInfo(LeaveReason reason)
        => AllReasons.FirstOrDefault(r => r.Reason == reason);
}
