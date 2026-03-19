using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class TicketRepository : Repository<Ticket>, ITicketRepository
{
    public TicketRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status)
        => await _dbSet
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .Include(t => t.Recipients)
                .ThenInclude(r => r.Employee)
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Ticket>> GetByCreatorAsync(int employeeId)
        => await _dbSet
            .Include(t => t.Creator)
            .Include(t => t.Recipients)
                .ThenInclude(r => r.Employee)
            .Where(t => t.CreatedBy == employeeId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Ticket>> GetByAssigneeAsync(int employeeId)
        => await _dbSet
            .Include(t => t.Creator)
            .Where(t => t.AssignedTo == employeeId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<Ticket?> GetWithFullDetailsAsync(int ticketId)
        => await _dbSet
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .Include(t => t.Recipients)
                .ThenInclude(r => r.Employee)
            .Include(t => t.Recipients)
                .ThenInclude(r => r.AddedBy)
            .Include(t => t.Messages.OrderBy(m => m.CreatedDate))
                .ThenInclude(m => m.Sender)
            .Include(t => t.Messages)
                .ThenInclude(m => m.Attachments)
                    .ThenInclude(a => a.UploadedBy)
            .Include(t => t.Attachments)
                .ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

    public async Task<IEnumerable<Ticket>> GetByRecipientAsync(int employeeId)
        => await _dbSet
            .Include(t => t.Creator)
            .Include(t => t.Recipients)
                .ThenInclude(r => r.Employee)
            .Include(t => t.Attachments)
            .Where(t => t.Recipients.Any(r => r.EmployeeId == employeeId))
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Ticket>> GetSentByCreatorAsync(int employeeId)
        => await _dbSet
            .Include(t => t.Creator)
            .Include(t => t.Recipients)
                .ThenInclude(r => r.Employee)
            .Include(t => t.Attachments)
            .Where(t => t.CreatedBy == employeeId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Ticket>> GetAllWithRecipientsAsync()
        => await _dbSet
            .Include(t => t.Creator)
            .Include(t => t.Assignee)
            .Include(t => t.Recipients)
                .ThenInclude(r => r.Employee)
            .Include(t => t.Attachments)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
}
