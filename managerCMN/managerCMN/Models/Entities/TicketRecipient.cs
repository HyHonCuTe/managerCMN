using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class TicketRecipient
{
    [Key]
    public int TicketRecipientId { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public TicketRecipientStatus Status { get; set; } = TicketRecipientStatus.Pending;

    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReadDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    // Who added this recipient (for forwarding tracking)
    public int? AddedById { get; set; }
    public Employee? AddedBy { get; set; }
}
