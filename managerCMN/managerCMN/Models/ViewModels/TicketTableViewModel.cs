using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class TicketTableViewModel
{
    public IEnumerable<Ticket> Tickets { get; set; } = Enumerable.Empty<Ticket>();
    public HashSet<int> StarredTicketIds { get; set; } = new();
}
