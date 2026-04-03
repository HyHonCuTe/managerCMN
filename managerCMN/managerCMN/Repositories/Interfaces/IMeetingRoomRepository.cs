using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IMeetingRoomRepository : IRepository<MeetingRoom>
{
    Task<IEnumerable<MeetingRoom>> GetActiveAsync();
    Task<IEnumerable<MeetingRoom>> GetAllOrderedAsync();
    Task<bool> NameExistsAsync(string name, int? ignoreId = null);
}
