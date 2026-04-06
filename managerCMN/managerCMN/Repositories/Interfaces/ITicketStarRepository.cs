using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface ITicketStarRepository : IRepository<TicketStar>
{
    Task<TicketStar?> GetByTicketAndEmployeeAsync(int ticketId, int employeeId);
    Task<HashSet<int>> GetStarredTicketIdsAsync(int employeeId);
}
