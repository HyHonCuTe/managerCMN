using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class Ticket
{
    [Key]
    public int TicketId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public int CreatedBy { get; set; }
    public Employee Creator { get; set; } = null!;

    public int? AssignedTo { get; set; }
    public Employee? Assignee { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedDate { get; set; }

    [MaxLength(2000)]
    public string? Resolution { get; set; }
}
