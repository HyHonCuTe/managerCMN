using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

/// <summary>Info about an approved request on a specific date</summary>
public class RequestDayInfo
{
    public int RequestId { get; set; }
    public RequestType RequestType { get; set; }
    public bool CountsAsWork { get; set; }
    /// <summary>true = only morning half, false = only afternoon half, null = full day</summary>
    public bool? IsHalfDayMorning { get; set; }
    /// <summary>true = fully approved, false = pending/partially approved</summary>
    public bool IsApproved { get; set; }
    public CheckInOutType? CheckInOutType { get; set; }
    public TimeOnly? RequestedTime { get; set; }

    public string TypeDisplayName => RequestType switch
    {
        RequestType.Leave => "Xin nghỉ",
        RequestType.CheckInOut => "Checkin/out",
        RequestType.Absence => "Vắng mặt",
        RequestType.WorkFromHome => "WFH",
        _ => "Khác"
    };

    public string TypeBadgeClass => RequestType switch
    {
        RequestType.Leave => "bg-warning-subtle text-warning",
        RequestType.CheckInOut => "bg-primary-subtle text-primary",
        RequestType.Absence => "bg-danger-subtle text-danger",
        RequestType.WorkFromHome => "bg-success-subtle text-success",
        _ => "bg-secondary-subtle text-secondary"
    };

    public string TypeIcon => RequestType switch
    {
        RequestType.Leave => "bi-calendar-x",
        RequestType.CheckInOut => "bi-clock-history",
        RequestType.Absence => "bi-person-x",
        RequestType.WorkFromHome => "bi-house-door",
        _ => "bi-file-earmark"
    };

    public string DetailDisplayName
        => RequestType == RequestType.CheckInOut
            ? $"{CheckInOutTypeHelper.GetDisplayName(CheckInOutType)}{(RequestedTime.HasValue ? $" {RequestedTime.Value:HH\\:mm}" : string.Empty)}"
            : TypeDisplayName;

    public static RequestDayInfo FromRequest(Request request, DateOnly date, bool isApproved)
    {
        var reqStart = DateOnly.FromDateTime(request.StartTime);
        var reqEnd = DateOnly.FromDateTime(request.EndTime);

        bool? isHalfDayMorning = null;
        if (date == reqStart && request.IsHalfDayStart)
            isHalfDayMorning = request.IsHalfDayStartMorning;
        else if (date == reqEnd && request.IsHalfDayEnd)
            isHalfDayMorning = request.IsHalfDayEndMorning;

        return new RequestDayInfo
        {
            RequestId = request.RequestId,
            RequestType = request.RequestType,
            CountsAsWork = request.CountsAsWork,
            IsHalfDayMorning = isHalfDayMorning,
            IsApproved = isApproved,
            CheckInOutType = request.CheckInOutType,
            RequestedTime = request.RequestType == RequestType.CheckInOut
                ? TimeOnly.FromDateTime(request.StartTime)
                : null
        };
    }

    private string GetCheckInOutTypeDisplayName() => CheckInOutType switch
    {
        Models.Enums.CheckInOutType.MissedCheckIn => "Quên check in",
        Models.Enums.CheckInOutType.MissedCheckOut => "Quên check out",
        _ => "Checkin/out"
    };
}

public class ShiftCoverage
{
    public bool Morning { get; set; }
    public bool Afternoon { get; set; }

    public decimal WorkPoints => AttendanceCalendarViewModel.GetWorkPoints(Morning, Afternoon);
}

public class AttendanceCoverageResult
{
    public TimeOnly? ActualCheckIn { get; set; }
    public TimeOnly? ActualCheckOut { get; set; }
    public TimeOnly? EffectiveCheckIn { get; set; }
    public TimeOnly? EffectiveCheckOut { get; set; }
    public TimeOnly? RequestedCheckIn { get; set; }
    public TimeOnly? RequestedCheckOut { get; set; }
    public bool UsedRequestedCheckIn { get; set; }
    public bool UsedRequestedCheckOut { get; set; }
    public bool HasMorning { get; set; }
    public bool HasAfternoon { get; set; }
    public bool HasEarlyCheckout { get; set; }
    public bool MissedMorningCutoff { get; set; }
    public bool HasApprovedLateArrivalRequest { get; set; }
    public bool HasApprovedEarlyLeaveRequest { get; set; }
    public bool LateArrivalNeedsApproval { get; set; }
    public bool EarlyLeaveNeedsApproval { get; set; }
    public decimal WorkPoints { get; set; }

    public bool HasDisplayTimeRange => EffectiveCheckIn.HasValue || EffectiveCheckOut.HasValue;
    public string DisplayCheckInText => EffectiveCheckIn?.ToString("H\\:mm") ?? "--:--";
    public string DisplayCheckOutText => EffectiveCheckOut?.ToString("H\\:mm") ?? "--:--";
}

public class AttendanceCalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? SelectedEmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
    public int? SelectedEmployeeJobTitleId { get; set; }

    /// <summary>Period start: 26th of previous month</summary>
    public DateOnly PeriodStart { get; set; }
    /// <summary>Period end: 25th of this month</summary>
    public DateOnly PeriodEnd { get; set; }

    public List<EmployeeSelectItem> Employees { get; set; } = [];
    public Dictionary<DateOnly, Attendance> AttendanceByDate { get; set; } = [];
    /// <summary>All active requests keyed by date (pending=yellow, approved=grey)</summary>
    public Dictionary<DateOnly, List<RequestDayInfo>> RequestsByDate { get; set; } = [];

    // Summary
    public decimal TotalWorkDays { get; set; }
    public decimal StandardWorkDays { get; set; }
    public decimal RequestWorkDays { get; set; }
    public int LateDays { get; set; }
    public int AbsentDays { get; set; }
    public decimal DeductionPoints { get; set; }

    // Shift constants
    public static readonly TimeOnly MorningStart = new(8, 30);
    public static readonly TimeOnly MorningEnd = new(12, 0);
    public static readonly TimeOnly AfternoonStart = new(13, 30);
    public static readonly TimeOnly AfternoonEnd = new(17, 30);
    /// <summary>Minimum checkout time to count afternoon session (4:00 PM)</summary>
    public static readonly TimeOnly MinAfternoonCheckOut = new(16, 0);
    /// <summary>Minimum checkout time on working Saturdays to count afternoon session (3:00 PM)</summary>
    public static readonly TimeOnly MinAfternoonCheckOutSaturday = new(15, 0);
    /// <summary>
    /// Get minimum checkout time to count afternoon session.
    /// Working Saturdays allow checkout from 15:00, other days keep 16:00.
    /// </summary>
    public static TimeOnly GetMinAfternoonCheckOut(DateOnly date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday && IsWorkSaturday(date)
            ? MinAfternoonCheckOutSaturday
            : MinAfternoonCheckOut;
    }

    /// <summary>Check if a Saturday is a work Saturday (alternating, anchor: 21/3/2026 = work)</summary>
    public static bool IsWorkSaturday(DateOnly date)
    {
        if (date.DayOfWeek != DayOfWeek.Saturday) return false;
        var anchor = new DateOnly(2026, 3, 21);
        var diffWeeks = (date.DayNumber - anchor.DayNumber) / 7;
        return diffWeeks % 2 == 0;
    }

    /// <summary>Check if a date is a working day (Mon-Fri always, Sat alternating, Sun never, excludes holidays)</summary>
    public static bool IsWorkingDay(DateOnly date, HashSet<DateOnly>? holidays = null)
    {
        if (date.DayOfWeek == DayOfWeek.Sunday) return false;
        if (date.DayOfWeek == DayOfWeek.Saturday) return IsWorkSaturday(date);
        if (holidays != null && holidays.Contains(date)) return false;  // NEW: Exclude holidays
        return true;
    }

    public static decimal GetWorkPoints(bool hasMorning, bool hasAfternoon)
    {
        if (hasMorning && hasAfternoon) return 1m;
        if (hasMorning || hasAfternoon) return 0.5m;
        return 0m;
    }

    public static Dictionary<DateOnly, List<RequestDayInfo>> BuildRequestCalendar(
        IEnumerable<Request> requests,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var result = new Dictionary<DateOnly, List<RequestDayInfo>>();

        foreach (var request in requests)
        {
            var reqStart = DateOnly.FromDateTime(request.StartTime);
            var reqEnd = DateOnly.FromDateTime(request.EndTime);
            var isApproved = request.Status == RequestStatus.FullyApproved;

            for (var date = reqStart; date <= reqEnd; date = date.AddDays(1))
            {
                if (date < periodStart || date > periodEnd)
                    continue;

                if (!result.ContainsKey(date))
                    result[date] = [];

                result[date].Add(RequestDayInfo.FromRequest(request, date, isApproved));
            }
        }

        return result;
    }

    public static ShiftCoverage GetApprovedRequestShiftCoverage(
        IEnumerable<RequestDayInfo>? requestInfos,
        bool includeCheckInOut = false,
        bool requireCountsAsWork = true)
    {
        var coverage = new ShiftCoverage();
        if (requestInfos == null)
            return coverage;

        foreach (var requestInfo in requestInfos.Where(r => r.IsApproved && (!requireCountsAsWork || r.CountsAsWork)))
        {
            if (!includeCheckInOut && requestInfo.RequestType == RequestType.CheckInOut)
                continue;

            if (requestInfo.IsHalfDayMorning == null)
            {
                coverage.Morning = true;
                coverage.Afternoon = true;
            }
            else if (requestInfo.IsHalfDayMorning == true)
            {
                coverage.Morning = true;
            }
            else
            {
                coverage.Afternoon = true;
            }
        }

        return coverage;
    }

    public static TimeSpan GetLateDuration(
        DateOnly date,
        Attendance? attendance,
        IEnumerable<RequestDayInfo>? requestInfos = null,
        AttendancePolicy? policy = null)
    {
        policy ??= AttendancePolicyHelper.Resolve(null);

        var correctedCoverage = EvaluateAttendanceCoverage(date, attendance, requestInfos, policy);
        if (!correctedCoverage.EffectiveCheckIn.HasValue)
            return TimeSpan.Zero;

        var scheduleCoverage = GetApprovedRequestShiftCoverage(
            requestInfos,
            includeCheckInOut: false,
            requireCountsAsWork: false);

        var effectiveCheckIn = correctedCoverage.EffectiveCheckIn.Value;

        if (scheduleCoverage.Morning)
        {
            if (correctedCoverage.HasAfternoon && effectiveCheckIn > AfternoonStart)
            {
                return effectiveCheckIn.ToTimeSpan() - AfternoonStart.ToTimeSpan();
            }

            return TimeSpan.Zero;
        }

        if (effectiveCheckIn > policy.LateThreshold)
        {
            return effectiveCheckIn.ToTimeSpan() - policy.LateThreshold.ToTimeSpan();
        }

        return TimeSpan.Zero;
    }

    public static AttendanceCoverageResult EvaluateAttendanceCoverage(
        DateOnly date,
        Attendance? attendance,
        IEnumerable<RequestDayInfo>? requestInfos = null,
        AttendancePolicy? policy = null)
    {
        policy ??= AttendancePolicyHelper.Resolve(null);

        var result = new AttendanceCoverageResult
        {
            ActualCheckIn = attendance?.CheckIn,
            ActualCheckOut = attendance?.CheckOut
        };

        var approvedCheckInOutRequests = requestInfos?
            .Where(r => r.IsApproved && r.CountsAsWork && r.RequestType == RequestType.CheckInOut)
            .ToList() ?? [];

        result.RequestedCheckIn = approvedCheckInOutRequests
            .Where(r => r.CheckInOutType == Models.Enums.CheckInOutType.MissedCheckIn && r.RequestedTime.HasValue)
            .Select(r => r.RequestedTime)
            .OrderBy(t => t)
            .FirstOrDefault();

        result.RequestedCheckOut = approvedCheckInOutRequests
            .Where(r => r.CheckInOutType == Models.Enums.CheckInOutType.MissedCheckOut && r.RequestedTime.HasValue)
            .Select(r => r.RequestedTime)
            .OrderByDescending(t => t)
            .FirstOrDefault();

        result.HasApprovedLateArrivalRequest = approvedCheckInOutRequests
            .Any(r => r.CheckInOutType == Models.Enums.CheckInOutType.LateArrival);

        result.HasApprovedEarlyLeaveRequest = approvedCheckInOutRequests
            .Any(r => r.CheckInOutType == Models.Enums.CheckInOutType.EarlyLeave);

        var effectiveCheckIn = result.ActualCheckIn;
        var effectiveCheckOut = result.ActualCheckOut;

        if (!effectiveCheckIn.HasValue && !effectiveCheckOut.HasValue
            && result.RequestedCheckIn.HasValue && result.RequestedCheckOut.HasValue
            && result.RequestedCheckIn.Value < result.RequestedCheckOut.Value)
        {
            effectiveCheckIn = result.RequestedCheckIn;
            effectiveCheckOut = result.RequestedCheckOut;
            result.UsedRequestedCheckIn = true;
            result.UsedRequestedCheckOut = true;
        }
        else if (attendance?.CheckIn.HasValue == true && attendance.CheckOut == null)
        {
            var singlePunch = attendance.CheckIn.Value;

            if (result.RequestedCheckIn.HasValue && result.RequestedCheckOut.HasValue)
            {
                if (singlePunch >= AfternoonStart)
                {
                    effectiveCheckIn = result.RequestedCheckIn;
                    effectiveCheckOut = singlePunch;
                    result.UsedRequestedCheckIn = true;
                }
                else
                {
                    effectiveCheckIn = singlePunch;
                    effectiveCheckOut = result.RequestedCheckOut;
                    result.UsedRequestedCheckOut = true;
                }
            }
            else if (result.RequestedCheckIn.HasValue)
            {
                effectiveCheckIn = result.RequestedCheckIn;
                effectiveCheckOut = singlePunch;
                result.UsedRequestedCheckIn = true;
            }
            else if (result.RequestedCheckOut.HasValue)
            {
                effectiveCheckIn = singlePunch;
                effectiveCheckOut = result.RequestedCheckOut;
                result.UsedRequestedCheckOut = true;
            }
        }
        else
        {
            if (!effectiveCheckIn.HasValue && effectiveCheckOut.HasValue && result.RequestedCheckIn.HasValue)
            {
                effectiveCheckIn = result.RequestedCheckIn;
                result.UsedRequestedCheckIn = true;
            }

            if (!effectiveCheckOut.HasValue && effectiveCheckIn.HasValue && result.RequestedCheckOut.HasValue)
            {
                effectiveCheckOut = result.RequestedCheckOut;
                result.UsedRequestedCheckOut = true;
            }
        }

        result.EffectiveCheckIn = effectiveCheckIn;
        result.EffectiveCheckOut = effectiveCheckOut;

        if (effectiveCheckIn.HasValue && effectiveCheckOut.HasValue)
        {
            var minAfternoonCheckOut = GetMinAfternoonCheckOut(date);
            var checkIn = effectiveCheckIn.Value;
            var checkOut = effectiveCheckOut.Value;
            var baseMorningCoverage = checkOut >= MorningStart;
            var baseAfternoonCoverage = checkIn <= AfternoonEnd
                && checkOut >= minAfternoonCheckOut;

            result.MissedMorningCutoff = policy.MissesMorningShift(checkIn);
            result.LateArrivalNeedsApproval = policy.RequiresLateArrivalRequest(checkIn)
                && !result.HasApprovedLateArrivalRequest;
            result.HasMorning = !result.MissedMorningCutoff
                && baseMorningCoverage
                && !result.LateArrivalNeedsApproval;

            result.EarlyLeaveNeedsApproval = policy.RequiresEarlyLeaveRequest(checkOut)
                && !result.HasApprovedEarlyLeaveRequest;
            result.HasAfternoon = baseAfternoonCoverage
                && !result.EarlyLeaveNeedsApproval;
            result.HasEarlyCheckout = result.EarlyLeaveNeedsApproval;
            result.WorkPoints = GetWorkPoints(result.HasMorning, result.HasAfternoon);
            return result;
        }

        if (attendance?.CheckIn.HasValue == true || attendance?.CheckOut.HasValue == true)
        {
            var singlePunch = result.EffectiveCheckIn ?? attendance?.CheckIn;
            if (singlePunch.HasValue)
            {
                result.MissedMorningCutoff = policy.MissesMorningShift(singlePunch.Value);
                result.LateArrivalNeedsApproval = policy.RequiresLateArrivalRequest(singlePunch.Value)
                    && !result.HasApprovedLateArrivalRequest;
                result.HasMorning = !result.MissedMorningCutoff
                    && !result.LateArrivalNeedsApproval;
                result.WorkPoints = result.HasMorning ? 0.5m : 0m;
            }
        }

        return result;
    }

    /// <summary>Period: month N/year = from (month-1)/26 to month/25</summary>
    public static (DateOnly start, DateOnly end) GetPeriodDates(int year, int month)
    {
        DateOnly start = month == 1
            ? new DateOnly(year - 1, 12, 26)
            : new DateOnly(year, month - 1, 26);
        var end = new DateOnly(year, month, 25);
        return (start, end);
    }

    /// <summary>
    /// Resolve the attendance period label for a specific date.
    /// From the 26th onward, the company starts counting for the next month.
    /// Example: 31/03/2026 belongs to attendance month 04/2026.
    /// </summary>
    public static (int year, int month) GetDisplayPeriod(DateOnly date)
    {
        if (date.Day >= 26)
        {
            return date.Month == 12
                ? (date.Year + 1, 1)
                : (date.Year, date.Month + 1);
        }

        return (date.Year, date.Month);
    }
}

public class EmployeeSelectItem
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
}
