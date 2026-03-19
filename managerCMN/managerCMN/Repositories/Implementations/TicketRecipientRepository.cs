using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class TicketRecipientRepository : Repository<TicketRecipient>, ITicketRecipientRepository
{
    public TicketRecipientRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TicketRecipient>> GetByTicketAsync(int ticketId)
        => await _dbSet
            .Include(tr => tr.Employee)
            .Include(tr => tr.AddedBy)
            .Where(tr => tr.TicketId == ticketId)
            .OrderBy(tr => tr.AddedDate)
            .ToListAsync();

    public async Task<TicketRecipient?> GetByTicketAndEmployeeAsync(int ticketId, int employeeId)
        => await _dbSet
            .Include(tr => tr.Employee)
            .FirstOrDefaultAsync(tr => tr.TicketId == ticketId && tr.EmployeeId == employeeId);
}
