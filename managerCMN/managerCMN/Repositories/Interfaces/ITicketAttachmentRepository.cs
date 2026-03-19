using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface ITicketAttachmentRepository : IRepository<TicketAttachment>
{
    Task<IEnumerable<TicketAttachment>> GetByTicketAsync(int ticketId);
    Task<IEnumerable<TicketAttachment>> GetByMessageAsync(int messageId);
}
