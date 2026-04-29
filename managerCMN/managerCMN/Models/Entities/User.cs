using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [MaxLength(100)]
    public string? GoogleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.VietnamNow;

    public DateTime? LastLogin { get; set; }

    // Navigation
    [MaxLength(50)]
    public string? TelegramChatId { get; set; }

    public bool TelegramMuteBroadcast { get; set; } = false;

    [MaxLength(500)]
    public string? TelegramDisabledNotificationTypes { get; set; }

    // Navigation
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
