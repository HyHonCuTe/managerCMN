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
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Ticket>> GetByCreatorAsync(int employeeId)
        => await _dbSet.Where(t => t.CreatedBy == employeeId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<Ticket>> GetByAssigneeAsync(int employeeId)
        => await _dbSet
            .Include(t => t.Creator)
            .Where(t => t.AssignedTo == employeeId)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();
}
