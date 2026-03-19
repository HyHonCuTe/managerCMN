using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface ITicketRecipientRepository : IRepository<TicketRecipient>
{
    Task<IEnumerable<TicketRecipient>> GetByTicketAsync(int ticketId);
    Task<TicketRecipient?> GetByTicketAndEmployeeAsync(int ticketId, int employeeId);
}
