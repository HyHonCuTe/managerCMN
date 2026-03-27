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

    public string TypeDisplayName => RequestType switch
    {
        RequestType.Leave => "Xin nghỉ",
        RequestType.CheckInOut => "Checkin/out",
        RequestType.Absence => "Vắng mặt",
        RequestType.WorkFromHome => "WFH",
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
}

public class AttendanceCalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? SelectedEmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }

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
    /// <summary>Late checkin threshold (10:00 AM) - if after this time and no request, don't count</summary>
    public static readonly TimeOnly LateCheckinThreshold = new(10, 0);

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

    /// <summary>Period: month N/year = from (month-1)/26 to month/25</summary>
    public static (DateOnly start, DateOnly end) GetPeriodDates(int year, int month)
    {
        DateOnly start = month == 1
            ? new DateOnly(year - 1, 12, 26)
            : new DateOnly(year, month - 1, 26);
        var end = new DateOnly(year, month, 25);
        return (start, end);
    }
}

public class EmployeeSelectItem
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
}
