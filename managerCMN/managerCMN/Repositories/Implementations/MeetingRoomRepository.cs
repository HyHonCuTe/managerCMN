using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class MeetingRoomRepository : Repository<MeetingRoom>, IMeetingRoomRepository
{
    public MeetingRoomRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<MeetingRoom>> GetActiveAsync()
        => await _dbSet
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();

    public async Task<IEnumerable<MeetingRoom>> GetAllOrderedAsync()
        => await _dbSet
            .AsNoTracking()
            .OrderBy(r => r.IsActive ? 0 : 1)
            .ThenBy(r => r.Name)
            .ToListAsync();

    public async Task<bool> NameExistsAsync(string name, int? ignoreId = null)
    {
        var normalized = name.Trim().ToLower();
        return await _dbSet.AnyAsync(r =>
            r.Name.ToLower() == normalized &&
            (!ignoreId.HasValue || r.MeetingRoomId != ignoreId.Value));
    }
}
