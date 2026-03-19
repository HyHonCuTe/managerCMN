using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class TicketAttachmentRepository : Repository<TicketAttachment>, ITicketAttachmentRepository
{
    public TicketAttachmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TicketAttachment>> GetByTicketAsync(int ticketId)
        => await _dbSet
            .Include(ta => ta.UploadedBy)
            .Where(ta => ta.TicketId == ticketId && ta.TicketMessageId == null)
            .OrderBy(ta => ta.UploadedDate)
            .ToListAsync();

    public async Task<IEnumerable<TicketAttachment>> GetByMessageAsync(int messageId)
        => await _dbSet
            .Include(ta => ta.UploadedBy)
            .Where(ta => ta.TicketMessageId == messageId)
            .OrderBy(ta => ta.UploadedDate)
            .ToListAsync();
}
