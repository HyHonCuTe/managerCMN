using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class Ticket
{
    [Key]
    public int TicketId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketUrgency Urgency { get; set; } = TicketUrgency.Normal;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime? Deadline { get; set; }

    public int CreatedBy { get; set; }
    public Employee Creator { get; set; } = null!;

    // Keep for backward compatibility (nullable)
    public int? AssignedTo { get; set; }
    public Employee? Assignee { get; set; }

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;

    public DateTime? ResolvedDate { get; set; }

    [MaxLength(2000)]
    public string? Resolution { get; set; }

    // Navigation properties
    public ICollection<TicketRecipient> Recipients { get; set; } = new List<TicketRecipient>();
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketStar> Stars { get; set; } = new List<TicketStar>();
}
