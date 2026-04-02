using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Helpers;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private const string FullAttendanceTableName = "FullAttendanceEmployees";

    private readonly IDashboardService _dashboardService;
    private readonly ILeaveService _leaveService;
    private readonly IAssetService _assetService;
    private readonly IPostHistoryService _postHistoryService;
    private readonly ApplicationDbContext _db;

    public DashboardController(IDashboardService dashboardService, ILeaveService leaveService,
        IAssetService assetService, IPostHistoryService postHistoryService, ApplicationDbContext db)
    {
        _dashboardService = dashboardService;
        _leaveService = leaveService;
        _assetService = assetService;
        _postHistoryService = postHistoryService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Manager");

        var model = new DashboardViewModel
        {
            TotalEmployees    = await _dashboardService.GetTotalEmployeesAsync(),
            PendingRequests   = await _dashboardService.GetPendingRequestsCountAsync(),
            ActiveTickets     = await _dashboardService.GetActiveTicketsCountAsync(),
            TotalAssets       = await _dashboardService.GetTotalAssetsAsync(),
            ExpiringContracts = await _dashboardService.GetExpiringContractsAsync()
        };

        // Add post history data for privileged users (Admin/Manager)
        if (isPrivileged)
        {
            var recentPosts = await _postHistoryService.GetRecentPostsAsync(10);
            var (totalPosts, totalRecordsProcessed, lastPostTime, successfulPosts, failedPosts) =
                await _postHistoryService.GetPostStatisticsAsync();

            model.PostHistory = new PostHistoryDashboardData
            {
                RecentPosts = recentPosts,
                TotalPosts = totalPosts,
                TotalRecordsProcessed = totalRecordsProcessed,
                LastPostTime = lastPostTime,
                SuccessfulPosts = successfulPosts,
                FailedPosts = failedPosts
            };
        }

        if (!isPrivileged)
        {
            var empIdClaim = User.FindFirst("EmployeeId");
            if (empIdClaim != null && int.TryParse(empIdClaim.Value, out var empId))
            {
                var employee = await _db.Employees
                    .Include(e => e.Position)
                    .Include(e => e.Department)
                    .Include(e => e.Contracts)
                    .FirstOrDefaultAsync(e => e.EmployeeId == empId);

                if (employee != null)
                {
                    var today = DateTimeHelper.VietnamToday;
                    var currentContract = employee.Contracts
                        .Where(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Renewed)
                        .OrderByDescending(c => c.StartDate)
                        .FirstOrDefault();

                    var attendanceSummary = await GetDashboardAttendanceSummaryAsync(empId);

                    var pendingCount = await _db.Set<Models.Entities.Request>()
                        .CountAsync(r => r.EmployeeId == empId && r.Status == RequestStatus.Pending);

                    // Get assigned assets count
                    var assignedAssetsCount = await _db.Set<Models.Entities.AssetAssignment>()
                        .CountAsync(aa => aa.EmployeeId == empId &&
                                         aa.Status == Models.Enums.AssetAssignmentStatus.Assigned);

                    model.Personal = new PersonalDashboardData
                    {
                        FullName                = employee.FullName,
                        PositionName            = employee.Position?.PositionName,
                        DepartmentName          = employee.Department?.DepartmentName,
                        CurrentContract         = currentContract,
                        LeaveSummary            = await _leaveService.GetBalanceSummaryAsync(empId),
                        AttendanceDaysWorked    = attendanceSummary.totalWorkDays,
                        AttendanceLateCount     = attendanceSummary.lateDays,
                        AttendanceOvertimeHours = attendanceSummary.overtimeHours,
                        AttendancePeriodMonth   = attendanceSummary.periodMonth,
                        AttendancePeriodYear    = attendanceSummary.periodYear,
                        MyPendingRequests       = pendingCount,
                        AssignedAssetsCount     = assignedAssetsCount
                    };
                }
            }
        }

        return View(model);
    }

    private static bool IsMissingFullAttendanceTable(SqlException ex)
        => ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
        && ex.Message.Contains(FullAttendanceTableName, StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsFullAttendanceEmployeeAsync(int employeeId)
    {
        try
        {
            return await _db.FullAttendanceEmployees.AnyAsync(e => e.EmployeeId == employeeId);
        }
        catch (SqlException ex) when (IsMissingFullAttendanceTable(ex))
        {
            return false;
        }
    }

    private async Task<(int periodYear, int periodMonth, decimal totalWorkDays, int lateDays, decimal overtimeHours)> GetDashboardAttendanceSummaryAsync(int employeeId)
    {
        var currentAttendancePeriod = AttendanceCalendarViewModel.GetDisplayPeriod(
            DateOnly.FromDateTime(DateTimeHelper.VietnamNow));
        var (periodStart, periodEnd) = AttendanceCalendarViewModel.GetPeriodDates(
            currentAttendancePeriod.year, currentAttendancePeriod.month);

        var attendances = await _db.Set<Models.Entities.Attendance>()
            .Where(a => a.EmployeeId == employeeId && a.Date >= periodStart && a.Date <= periodEnd)
            .ToListAsync();
        var attendanceByDate = attendances.ToDictionary(a => a.Date);

        var activeRequests = await _db.Set<Models.Entities.Request>()
            .Where(r => r.EmployeeId == employeeId
                && r.Status != RequestStatus.Rejected
                && r.Status != RequestStatus.Cancelled)
            .ToListAsync();

        var requestsByDate = new Dictionary<DateOnly, List<RequestDayInfo>>();
        foreach (var req in activeRequests)
        {
            var reqStart = DateOnly.FromDateTime(req.StartTime);
            var reqEnd = DateOnly.FromDateTime(req.EndTime);
            var isApproved = req.Status == RequestStatus.FullyApproved;

            for (var d = reqStart; d <= reqEnd; d = d.AddDays(1))
            {
                if (d < periodStart || d > periodEnd)
                    continue;

                if (!requestsByDate.ContainsKey(d))
                    requestsByDate[d] = [];

                bool? isHalfDayMorning = null;
                if (d == reqStart && req.IsHalfDayStart)
                    isHalfDayMorning = req.IsHalfDayStartMorning;
                else if (d == reqEnd && req.IsHalfDayEnd)
                    isHalfDayMorning = req.IsHalfDayEndMorning;

                requestsByDate[d].Add(new RequestDayInfo
                {
                    RequestId = req.RequestId,
                    RequestType = req.RequestType,
                    CountsAsWork = req.CountsAsWork,
                    IsHalfDayMorning = isHalfDayMorning,
                    IsApproved = isApproved
                });
            }
        }

        var morningStart = AttendanceCalendarViewModel.MorningStart;
        var morningEnd = AttendanceCalendarViewModel.MorningEnd;
        var afternoonStart = AttendanceCalendarViewModel.AfternoonStart;
        var afternoonEnd = AttendanceCalendarViewModel.AfternoonEnd;
        var lateCheckinThreshold = AttendanceCalendarViewModel.LateCheckinThreshold;

        decimal attendanceWorkDays = 0m;
        int lateDays = 0;

        foreach (var att in attendances)
        {
            var hasApprovedRequest = requestsByDate.ContainsKey(att.Date)
                && requestsByDate[att.Date].Any(r => r.IsApproved && r.CountsAsWork);

            bool isLateCheckin = att.CheckIn.HasValue && att.CheckIn.Value > lateCheckinThreshold;
            if (isLateCheckin && !hasApprovedRequest)
                continue;

            if (att.CheckIn.HasValue && att.CheckOut.HasValue)
            {
                var minAfternoonCheckOut = AttendanceCalendarViewModel.GetMinAfternoonCheckOut(att.Date);
                bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
                bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= minAfternoonCheckOut;

                if (hasMorning && hasAfternoon)
                    attendanceWorkDays += 1m;
                else if (hasMorning || hasAfternoon)
                    attendanceWorkDays += 0.5m;
            }
            else if (att.CheckIn.HasValue)
            {
                attendanceWorkDays += 0.5m;
            }

            if (att.IsLate)
                lateDays++;
        }

        decimal requestWorkDays = 0m;
        foreach (var kvp in requestsByDate)
        {
            var date = kvp.Key;
            if (!AttendanceCalendarViewModel.IsWorkingDay(date))
                continue;

            var approvedReqs = kvp.Value.Where(r => r.IsApproved).ToList();
            if (!approvedReqs.Any())
                continue;

            bool hasAttendance = attendanceByDate.ContainsKey(date);
            decimal attendanceWorkForDate = 0m;
            if (hasAttendance)
            {
                var att = attendanceByDate[date];
                if (att.CheckIn.HasValue && att.CheckOut.HasValue)
                {
                    var minAfternoonCheckOut = AttendanceCalendarViewModel.GetMinAfternoonCheckOut(date);
                    bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
                    bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= minAfternoonCheckOut;

                    if (hasMorning && hasAfternoon) attendanceWorkForDate = 1m;
                    else if (hasMorning || hasAfternoon) attendanceWorkForDate = 0.5m;
                }
                else if (att.CheckIn.HasValue)
                {
                    attendanceWorkForDate = 0.5m;
                }
            }

            bool morningCoveredByRequest = false;
            bool afternoonCoveredByRequest = false;
            foreach (var reqInfo in approvedReqs)
            {
                if (!reqInfo.CountsAsWork) continue;

                if (reqInfo.IsHalfDayMorning == null)
                {
                    morningCoveredByRequest = true;
                    afternoonCoveredByRequest = true;
                }
                else if (reqInfo.IsHalfDayMorning == true)
                {
                    morningCoveredByRequest = true;
                }
                else
                {
                    afternoonCoveredByRequest = true;
                }
            }

            decimal requestCong = 0m;
            if (morningCoveredByRequest && afternoonCoveredByRequest) requestCong = 1m;
            else if (morningCoveredByRequest || afternoonCoveredByRequest) requestCong = 0.5m;

            decimal totalForDate = Math.Min(attendanceWorkForDate + requestCong, 1m);
            decimal extraFromRequest = totalForDate - attendanceWorkForDate;
            if (extraFromRequest > 0)
                requestWorkDays += extraFromRequest;
        }

        var overtimeHours = attendances.Sum(a => a.OvertimeHours ?? 0m);
        var totalWorkDays = attendanceWorkDays + requestWorkDays;

        if (await IsFullAttendanceEmployeeAsync(employeeId))
        {
            var holidays = await _db.Holidays
                .Where(h => h.Date >= periodStart && h.Date <= periodEnd)
                .Select(h => h.Date)
                .ToHashSetAsync();

            decimal autoWorkDays = 0m;
            var todayDate = DateOnly.FromDateTime(DateTimeHelper.VietnamNow);
            for (var d = periodStart; d <= periodEnd && d <= todayDate; d = d.AddDays(1))
            {
                if (AttendanceCalendarViewModel.IsWorkingDay(d, holidays))
                    autoWorkDays += 1m;
            }

            totalWorkDays = autoWorkDays;
            lateDays = 0;
        }

        return (currentAttendancePeriod.year, currentAttendancePeriod.month, totalWorkDays, lateDays, overtimeHours);
    }
}
