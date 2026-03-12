using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class SystemLog
{
    [Key]
    public long LogId { get; set; }

    public int? UserId { get; set; }

    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Module { get; set; }

    public string? DataBefore { get; set; }

    public string? DataAfter { get; set; }

    [MaxLength(50)]
    public string? IPAddress { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
