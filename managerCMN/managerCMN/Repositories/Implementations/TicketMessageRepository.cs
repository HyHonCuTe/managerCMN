using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class TicketMessageRepository : Repository<TicketMessage>, ITicketMessageRepository
{
    public TicketMessageRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TicketMessage>> GetByTicketAsync(int ticketId)
        => await _dbSet
            .Include(tm => tm.Sender)
            .Include(tm => tm.Attachments)
            .Where(tm => tm.TicketId == ticketId)
            .OrderBy(tm => tm.CreatedDate)
            .ToListAsync();

    public async Task<TicketMessage?> GetWithAttachmentsAsync(int messageId)
        => await _dbSet
            .Include(tm => tm.Attachments)
                .ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(tm => tm.TicketMessageId == messageId);
}
