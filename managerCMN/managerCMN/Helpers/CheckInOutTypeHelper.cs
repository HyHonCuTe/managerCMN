using managerCMN.Models.Enums;

namespace managerCMN.Helpers;

public static class CheckInOutTypeHelper
{
    public static bool UsesCheckInClock(CheckInOutType? type)
        => type is CheckInOutType.MissedCheckIn or CheckInOutType.LateArrival;

    public static bool UsesCheckOutClock(CheckInOutType? type)
        => type is CheckInOutType.MissedCheckOut or CheckInOutType.EarlyLeave;

    public static bool IsMissedCheckType(CheckInOutType? type)
        => type is null or CheckInOutType.MissedCheckIn or CheckInOutType.MissedCheckOut;

    public static bool IsLateOrEarlyType(CheckInOutType? type)
        => type is CheckInOutType.LateArrival or CheckInOutType.EarlyLeave;

    public static string GetDisplayName(CheckInOutType? type) => type switch
    {
        CheckInOutType.MissedCheckIn => "Quên check in",
        CheckInOutType.MissedCheckOut => "Quên check out",
        CheckInOutType.LateArrival => "Đi trễ",
        CheckInOutType.EarlyLeave => "Về sớm",
        _ => "Checkin/out"
    };
}