using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class PostHistoryRepository : Repository<PostHistory>, IPostHistoryRepository
{
    public PostHistoryRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<PostHistory>> GetRecentPostsAsync(int limit)
        => await _dbSet
            .OrderByDescending(ph => ph.CreatedAt)
            .Take(limit)
            .ToListAsync();

    public async Task<IEnumerable<PostHistory>> GetPostsByDateRangeAsync(DateTime startDate, DateTime endDate)
        => await _dbSet
            .Where(ph => ph.CreatedAt >= startDate && ph.CreatedAt <= endDate)
            .OrderByDescending(ph => ph.CreatedAt)
            .ToListAsync();

    public async Task<(int TotalPosts, int TotalRecordsProcessed, DateTime? LastPostTime, int SuccessfulPosts, int FailedPosts)>
        GetPostStatisticsAsync()
    {
        if (!await _dbSet.AnyAsync())
        {
            return (0, 0, null, 0, 0);
        }

        var totalPosts = await _dbSet.CountAsync();
        var totalRecordsProcessed = await _dbSet.SumAsync(ph => ph.ProcessedCount);
        var lastPostTime = await _dbSet.MaxAsync(ph => (DateTime?)ph.CreatedAt);
        var successfulPosts = await _dbSet.CountAsync(ph => ph.IsSuccess);
        var failedPosts = totalPosts - successfulPosts;

        return (totalPosts, totalRecordsProcessed, lastPostTime, successfulPosts, failedPosts);
    }
}