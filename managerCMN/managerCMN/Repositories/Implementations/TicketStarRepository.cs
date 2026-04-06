using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class TicketStarRepository : Repository<TicketStar>, ITicketStarRepository
{
    public TicketStarRepository(ApplicationDbContext context) : base(context) { }

    public async Task<TicketStar?> GetByTicketAndEmployeeAsync(int ticketId, int employeeId)
        => await _dbSet.FirstOrDefaultAsync(ts => ts.TicketId == ticketId && ts.EmployeeId == employeeId);

    public async Task<HashSet<int>> GetStarredTicketIdsAsync(int employeeId)
        => await _dbSet
            .Where(ts => ts.EmployeeId == employeeId)
            .Select(ts => ts.TicketId)
            .ToHashSetAsync();
}
