namespace managerCMN.Helpers;

public static class DateTimeHelper
{
    // Vietnam timezone (UTC+7)
    private static readonly TimeZoneInfo VietnamTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

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
}
