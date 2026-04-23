namespace managerCMN.Helpers;

public static class DateTimeHelper
{
    // Vietnam timezone (UTC+7) — tries Windows ID first, then Linux/macOS IANA ID
    private static readonly TimeZoneInfo VietnamTimeZone = LoadVietnamTz();

    private static TimeZoneInfo LoadVietnamTz()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { }
        }
        return TimeZoneInfo.Utc;
    }

    /// <summary>
    /// Convert UTC DateTime to Vietnam timezone (UTC+7)
    /// </summary>
    public static DateTime ToVietnamTime(this DateTime utcDateTime)
    {
        // If the datetime is already local, convert to UTC first
        if (utcDateTime.Kind == DateTimeKind.Local)
            utcDateTime = utcDateTime.ToUniversalTime();
        else if (utcDateTime.Kind == DateTimeKind.Unspecified)
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
    }

    /// <summary>
    /// Get current time in Vietnam timezone
    /// </summary>
    public static DateTime VietnamNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);

    /// <summary>
    /// Get today's date in Vietnam timezone
    /// </summary>
    public static DateTime VietnamToday => VietnamNow.Date;

    /// <summary>
    /// Convert nullable DateTime to Vietnam time
    /// </summary>
    public static DateTime? ToVietnamTime(this DateTime? utcDateTime)
    {
        return utcDateTime?.ToVietnamTime();
    }

    /// <summary>
    /// Count working days between two dates (excluding Saturday and Sunday)
    /// </summary>
    /// <param name="fromDate">Start date (inclusive)</param>
    /// <param name="toDate">End date (inclusive)</param>
    /// <returns>Number of working days (Monday to Friday only)</returns>
    public static int CountWorkingDaysBetween(DateTime fromDate, DateTime toDate)
    {
        // Ensure fromDate <= toDate for backward counting
        if (fromDate > toDate)
        {
            var temp = fromDate;
            fromDate = toDate;
            toDate = temp;
        }

        int workingDays = 0;
        for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
        {
            // Skip Saturday and Sunday
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        return workingDays;
    }
}
