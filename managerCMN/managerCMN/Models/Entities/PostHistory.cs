using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

/// <summary>
/// Tracks API post history for attendance data synchronization from biometric devices
/// </summary>
public class PostHistory
{
    [Key]
    public long PostHistoryId { get; set; }

    /// <summary>
    /// Number of punch records received in this API call
    /// </summary>
    public int RecordsCount { get; set; }

    /// <summary>
    /// Number of successfully processed records
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// IP address of the client that made the request
    /// </summary>
    [MaxLength(50)]
    public string? IPAddress { get; set; }

    /// <summary>
    /// User agent of the client (e.g., AttendanceSync/1.0)
    /// </summary>
    [MaxLength(200)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Whether the API call was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the API call failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time range of the earliest punch record in this batch
    /// </summary>
    public DateTime? EarliestPunchTime { get; set; }

    /// <summary>
    /// Time range of the latest punch record in this batch
    /// </summary>
    public DateTime? LatestPunchTime { get; set; }

    /// <summary>
    /// Comma-separated list of employee info in format "Name (#AttendanceCode)"
    /// e.g., "KhoiMX (#4), HoangNV (#5)"
    /// </summary>
    [MaxLength(2000)]
    public string? EmployeeInfo { get; set; }

    /// <summary>
    /// When this API call was received - stored as local time
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTimeHelper.VietnamNow;
}