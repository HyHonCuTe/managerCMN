using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IPostHistoryService
{
    /// <summary>
    /// Log a new API post event
    /// </summary>
    Task LogApiPostAsync(int recordsCount, int processedCount, string? ipAddress, string? userAgent,
                        bool isSuccess, string? errorMessage = null, DateTime? earliestPunchTime = null,
                        DateTime? latestPunchTime = null, string? employeeInfo = null);

    /// <summary>
    /// Get the most recent API post history records
    /// </summary>
    Task<IEnumerable<PostHistory>> GetRecentPostsAsync(int limit = 10);

    /// <summary>
    /// Get post history statistics
    /// </summary>
    Task<(int TotalPosts, int TotalRecordsProcessed, DateTime? LastPostTime, int SuccessfulPosts, int FailedPosts)> GetPostStatisticsAsync();

    /// <summary>
    /// Get post history by date range
    /// </summary>
    Task<IEnumerable<PostHistory>> GetPostsByDateRangeAsync(DateTime startDate, DateTime endDate);
}