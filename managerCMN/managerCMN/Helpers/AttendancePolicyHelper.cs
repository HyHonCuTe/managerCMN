using managerCMN.Models.Enums;

namespace managerCMN.Helpers;

public sealed record AttendancePolicy(
    TimeOnly LateThreshold,
    TimeOnly LateRequestWindowStart,
    TimeOnly LateRequestWindowEnd,
    TimeOnly MorningCutoff,
    TimeOnly EarlyLeaveRequestWindowStart,
    TimeOnly NoRequestCheckoutThreshold,
    int MonthlyMissedCheckInOutRequestLimit,
    int MonthlyLateEarlyRequestLimit)
{
    public bool RequiresLateArrivalRequest(TimeOnly checkIn)
        => checkIn >= LateRequestWindowStart && checkIn <= LateRequestWindowEnd;

    public bool MissesMorningShift(TimeOnly checkIn)
        => checkIn > MorningCutoff;

    public bool RequiresEarlyLeaveRequest(TimeOnly checkOut)
        => checkOut >= EarlyLeaveRequestWindowStart && checkOut < NoRequestCheckoutThreshold;

    public int GetMonthlyCheckInOutLimit(CheckInOutType? type)
        => CheckInOutTypeHelper.IsLateOrEarlyType(type)
            ? MonthlyLateEarlyRequestLimit
            : MonthlyMissedCheckInOutRequestLimit;
}

public static class AttendancePolicyHelper
{
    private static readonly AttendancePolicy LeadershipPolicy = new(
        LateThreshold: new TimeOnly(9, 0),
        LateRequestWindowStart: new TimeOnly(9, 0),
        LateRequestWindowEnd: new TimeOnly(10, 0),
        MorningCutoff: new TimeOnly(10, 0),
        EarlyLeaveRequestWindowStart: new TimeOnly(16, 0),
        NoRequestCheckoutThreshold: new TimeOnly(17, 0),
        MonthlyMissedCheckInOutRequestLimit: 5,
        MonthlyLateEarlyRequestLimit: 10);

    private static readonly AttendancePolicy StandardPolicy = new(
        LateThreshold: new TimeOnly(8, 45),
        LateRequestWindowStart: new TimeOnly(8, 45),
        LateRequestWindowEnd: new TimeOnly(10, 0),
        MorningCutoff: new TimeOnly(10, 0),
        EarlyLeaveRequestWindowStart: new TimeOnly(16, 0),
        NoRequestCheckoutThreshold: new TimeOnly(17, 30),
        MonthlyMissedCheckInOutRequestLimit: 5,
        MonthlyLateEarlyRequestLimit: 5);

    public static AttendancePolicy Resolve(int? jobTitleId)
        => IsLeadership(jobTitleId) ? LeadershipPolicy : StandardPolicy;

    public static bool IsLeadership(int? jobTitleId)
        => jobTitleId is 1 or 2;
}
