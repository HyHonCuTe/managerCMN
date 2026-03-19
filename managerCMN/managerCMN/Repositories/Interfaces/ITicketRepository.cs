using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Repositories.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    // Existing methods (backward compat)
    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetByCreatorAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetByAssigneeAsync(int employeeId);

    // New email-like functionality
    Task<Ticket?> GetWithFullDetailsAsync(int ticketId);
    Task<IEnumerable<Ticket>> GetByRecipientAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetSentByCreatorAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetAllWithRecipientsAsync();
}
