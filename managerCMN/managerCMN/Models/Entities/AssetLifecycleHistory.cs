using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class AssetLifecycleHistory
{
    [Key]
    public int HistoryId { get; set; }

    public int AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public AssetLifecycleEventType EventType { get; set; }
    public DateTime EventDate { get; set; } = DateTime.UtcNow;

    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int? PerformedById { get; set; }
    public Employee? PerformedBy { get; set; }

    [MaxLength(1000)]
    public string? EventDescription { get; set; }

    [MaxLength(500)]
    public string? PreviousValue { get; set; }

    [MaxLength(500)]
    public string? NewValue { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}