using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User?> GetWithRolesAsync(int userId);
}
