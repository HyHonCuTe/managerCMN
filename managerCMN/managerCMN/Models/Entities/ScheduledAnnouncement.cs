using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ScheduledAnnouncement
{
    [Key]
    public int AnnouncementId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }

    public AnnouncementStatus Status { get; set; } = AnnouncementStatus.Pending;

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTimeHelper.VietnamNow;

    public int CreatedByEmployeeId { get; set; }

    // Target filters — null means "no filter on this dimension"
    // If FilterEmployeeIds is non-null, only those specific employees are targeted.
    // Otherwise, all active employees are included and optional DepartmentId/Gender filters narrow the list.
    public int? FilterDepartmentId { get; set; }

    public int? FilterGender { get; set; } // maps to Gender enum value

    [MaxLength(2000)]
    public string? FilterEmployeeIds { get; set; } // JSON: [1, 2, 3]

    // Navigation
    public Department? FilterDepartment { get; set; }
}
