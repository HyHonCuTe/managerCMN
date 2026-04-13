using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class TicketIndexViewModel
{
    public IEnumerable<Ticket> SentTickets { get; set; } = Enumerable.Empty<Ticket>();
    public IEnumerable<Ticket> ReceivedTickets { get; set; } = Enumerable.Empty<Ticket>();
    public IEnumerable<Ticket> ExpiredTickets { get; set; } = Enumerable.Empty<Ticket>();
    public IEnumerable<Ticket> AllTickets { get; set; } = Enumerable.Empty<Ticket>();
    public HashSet<int> StarredTicketIds { get; set; } = new();

    public bool IsAdmin { get; set; }
    public string ActiveTab { get; set; } = "received";

    // Filter options
    public TicketStatus? FilterStatus { get; set; }
    public TicketUrgency? FilterUrgency { get; set; }
}
