using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Repositories.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetByCreatorAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetByAssigneeAsync(int employeeId);
}
