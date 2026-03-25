namespace managerCMN.Helpers;

/// <summary>
/// Helper class for handling Vietnam timezone operations
/// </summary>
public static class VietnamTimeHelper
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>
    /// Get current Vietnam time
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTime.Now, VietnamTimeZone);

    /// <summary>
    /// Convert UTC time to Vietnam time
    /// </summary>
    public static DateTime FromUtc(DateTime utcTime) => TimeZoneInfo.ConvertTimeFromUtc(utcTime, VietnamTimeZone);

    /// <summary>
    /// Convert any DateTime to Vietnam time and mark as Unspecified to avoid EF timezone conversion
    /// </summary>
    public static DateTime ToVietnamUnspecified(DateTime dateTime)
    {
        DateTime vietnamTime;

        if (dateTime.Kind == DateTimeKind.Utc)
        {
            vietnamTime = FromUtc(dateTime);
        }
        else if (dateTime.Kind == DateTimeKind.Local)
        {
            // Convert local time to Vietnam time (in case server is in different timezone)
            vietnamTime = TimeZoneInfo.ConvertTime(dateTime, VietnamTimeZone);
        }
        else
        {
            // Already unspecified, assume it's Vietnam time
            vietnamTime = dateTime;
        }

        // Return as Unspecified to prevent EF from doing timezone conversion
        return DateTime.SpecifyKind(vietnamTime, DateTimeKind.Unspecified);
    }
}