using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IPostHistoryRepository : IRepository<PostHistory>
{
    /// <summary>
    /// Get the most recent post history records
    /// </summary>
    Task<IEnumerable<PostHistory>> GetRecentPostsAsync(int limit);

    /// <summary>
    /// Get post history by date range
    /// </summary>
    Task<IEnumerable<PostHistory>> GetPostsByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get post statistics
    /// </summary>
    Task<(int TotalPosts, int TotalRecordsProcessed, DateTime? LastPostTime, int SuccessfulPosts, int FailedPosts)> GetPostStatisticsAsync();
}