using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Message { get; set; }

    [MaxLength(500)]
    public string? TargetUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;
}
