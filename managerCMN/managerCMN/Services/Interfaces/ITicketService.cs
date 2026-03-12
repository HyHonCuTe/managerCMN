using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Services.Interfaces;

public interface ITicketService
{
    Task<IEnumerable<Ticket>> GetAllAsync();
    Task<Ticket?> GetByIdAsync(int id);
    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetByCreatorAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetByAssigneeAsync(int employeeId);
    Task CreateAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task AssignAsync(int ticketId, int assigneeId);
    Task ResolveAsync(int ticketId, string resolution);
    Task CloseAsync(int ticketId);
}
