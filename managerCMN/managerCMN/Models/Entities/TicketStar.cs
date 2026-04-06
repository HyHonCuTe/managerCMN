using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class TicketStar
{
    [Key]
    public int TicketStarId { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateTime StarredAt { get; set; } = DateTime.UtcNow;
}
