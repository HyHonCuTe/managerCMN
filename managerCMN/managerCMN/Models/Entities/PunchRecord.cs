using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

/// <summary>
/// Stores individual punch records from biometric devices.
/// Multiple punch records per employee per day are preserved for audit trail.
/// </summary>
public class PunchRecord
{
    [Key]
    public int PunchRecordId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>
    /// Date of the punch (extracted from SourceTimestamp)
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Time of the punch (extracted from SourceTimestamp)
    /// </summary>
    public TimeOnly PunchTime { get; set; }

    /// <summary>
    /// Full timestamp from the source device (for audit trail)
    /// </summary>
    public DateTime SourceTimestamp { get; set; }

    /// <summary>
    /// Auto-incremented sequence number for this employee on this date (1, 2, 3, ...)
    /// First punch = 1, second = 2, etc.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Device ID or source identifier (optional, for multi-device tracking)
    /// </summary>
    [MaxLength(50)]
    public string? DeviceId { get; set; }

    /// <summary>
    /// When this record was created in the database - stored as local time
    /// </summary>
    public DateTime CreatedAt { get; set; } = VietnamTimeHelper.Now;
}
