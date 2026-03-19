using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface ITicketMessageRepository : IRepository<TicketMessage>
{
    Task<IEnumerable<TicketMessage>> GetByTicketAsync(int ticketId);
    Task<TicketMessage?> GetWithAttachmentsAsync(int messageId);
}
