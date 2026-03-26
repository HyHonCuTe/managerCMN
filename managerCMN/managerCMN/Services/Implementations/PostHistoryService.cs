using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using managerCMN.Helpers;

namespace managerCMN.Services.Implementations;

public class PostHistoryService : IPostHistoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public PostHistoryService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task LogApiPostAsync(int recordsCount, int processedCount, string? ipAddress, string? userAgent,
                                    bool isSuccess, string? errorMessage = null, DateTime? earliestPunchTime = null,
                                    DateTime? latestPunchTime = null, string? employeeInfo = null)
    {
        var postHistory = new PostHistory
        {
            RecordsCount = recordsCount,
            ProcessedCount = processedCount,
            IPAddress = ipAddress?[..Math.Min(ipAddress.Length, 50)], // Truncate to max length
            UserAgent = userAgent?[..Math.Min(userAgent.Length, 200)], // Truncate to max length
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage?[..Math.Min(errorMessage.Length, 500)], // Truncate to max length
            EarliestPunchTime = earliestPunchTime,
            LatestPunchTime = latestPunchTime,
            EmployeeInfo = employeeInfo?[..Math.Min(employeeInfo.Length, 2000)], // Truncate to max length
            CreatedAt = VietnamTimeHelper.Now
        };

        await _unitOfWork.PostHistories.AddAsync(postHistory);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<PostHistory>> GetRecentPostsAsync(int limit = 10)
        => await _unitOfWork.PostHistories.GetRecentPostsAsync(limit);

    public async Task<(int TotalPosts, int TotalRecordsProcessed, DateTime? LastPostTime, int SuccessfulPosts, int FailedPosts)>
        GetPostStatisticsAsync()
        => await _unitOfWork.PostHistories.GetPostStatisticsAsync();

    public async Task<IEnumerable<PostHistory>> GetPostsByDateRangeAsync(DateTime startDate, DateTime endDate)
        => await _unitOfWork.PostHistories.GetPostsByDateRangeAsync(startDate, endDate);
}