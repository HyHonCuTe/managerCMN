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
    private const string MasterAdminEmployeeCode = "A00000";
    private const int UpcomingBirthdayReminderDays = 20;

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
        var isMasterAdmin = User.IsInRole("Admin") && User.HasClaim("EmployeeCode", MasterAdminEmployeeCode);

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

        if (isMasterAdmin)
            model.UpcomingBirthdays = await GetUpcomingBirthdaysAsync(UpcomingBirthdayReminderDays);

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
                        RequestedLeaveDaysThisMonth = await GetDashboardLeaveDaysThisMonthAsync(empId),
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
        var employeeJobTitleId = await _db.Employees
            .Where(e => e.EmployeeId == employeeId)
            .Select(e => e.JobTitleId)
            .FirstOrDefaultAsync();
        var attendancePolicy = AttendancePolicyHelper.Resolve(employeeJobTitleId);

        var attendances = await _db.Set<Models.Entities.Attendance>()
            .Where(a => a.EmployeeId == employeeId && a.Date >= periodStart && a.Date <= periodEnd)
            .ToListAsync();
        var attendanceByDate = attendances.ToDictionary(a => a.Date);

        var activeRequests = await _db.Set<Models.Entities.Request>()
            .Where(r => r.EmployeeId == employeeId
                && r.Status != RequestStatus.Rejected
                && r.Status != RequestStatus.Cancelled)
            .ToListAsync();
        var requestsByDate = AttendanceCalendarViewModel.BuildRequestCalendar(activeRequests, periodStart, periodEnd);
        var holidays = await _db.Holidays
            .Where(h => h.Date >= periodStart && h.Date <= periodEnd)
            .Select(h => h.Date)
            .ToHashSetAsync();

        decimal attendanceWorkDays = 0m;
        decimal requestWorkDays = 0m;
        int lateDays = 0;

        foreach (var date in Enumerable.Range(0, periodEnd.DayNumber - periodStart.DayNumber + 1)
                     .Select(offset => periodStart.AddDays(offset)))
        {
            if (!AttendanceCalendarViewModel.IsWorkingDay(date, holidays))
                continue;

            var attendance = attendanceByDate.GetValueOrDefault(date);
            var approvedReqs = requestsByDate.GetValueOrDefault(date)?
                .Where(r => r.IsApproved)
                .ToList() ?? [];

            var rawCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(date, attendance, policy: attendancePolicy);
            var correctedCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(date, attendance, approvedReqs, attendancePolicy);
            var requestCoverage = AttendanceCalendarViewModel.GetApprovedRequestShiftCoverage(approvedReqs);
            var finalPoints = AttendanceCalendarViewModel.GetWorkPoints(
                correctedCoverage.HasMorning || requestCoverage.Morning,
                correctedCoverage.HasAfternoon || requestCoverage.Afternoon);

            attendanceWorkDays += rawCoverage.WorkPoints;

            var extraFromRequest = finalPoints - rawCoverage.WorkPoints;
            if (extraFromRequest > 0)
                requestWorkDays += extraFromRequest;

            if (AttendanceCalendarViewModel.GetLateDuration(date, attendance, approvedReqs, attendancePolicy) > TimeSpan.Zero)
                lateDays++;
        }

        var overtimeHours = attendances.Sum(a => a.OvertimeHours ?? 0m);
        var totalWorkDays = attendanceWorkDays + requestWorkDays;

        if (await IsFullAttendanceEmployeeAsync(employeeId))
        {
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

    private async Task<decimal> GetDashboardLeaveDaysThisMonthAsync(int employeeId)
    {
        var today = DateTimeHelper.VietnamToday;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var monthStartDateTime = monthStart.ToDateTime(TimeOnly.MinValue);
        var nextMonthStartDateTime = monthStart.AddMonths(1).ToDateTime(TimeOnly.MinValue);

        var leaveRequests = await _db.Set<Models.Entities.Request>()
            .Where(r => r.EmployeeId == employeeId
                && r.RequestType == RequestType.Leave
                && r.Status != RequestStatus.Rejected
                && r.Status != RequestStatus.Cancelled
                && r.StartTime < nextMonthStartDateTime
                && r.EndTime >= monthStartDateTime)
            .ToListAsync();

        if (leaveRequests.Count == 0)
            return 0m;

        var holidays = await _db.Holidays
            .Where(h => h.Date >= monthStart && h.Date <= monthEnd)
            .Select(h => h.Date)
            .ToHashSetAsync();

        decimal totalLeaveDays = 0m;
        foreach (var request in leaveRequests)
        {
            var requestStart = DateOnly.FromDateTime(request.StartTime);
            var requestEnd = DateOnly.FromDateTime(request.EndTime);
            var overlapStart = requestStart < monthStart ? monthStart : requestStart;
            var overlapEnd = requestEnd > monthEnd ? monthEnd : requestEnd;

            for (var date = overlapStart; date <= overlapEnd; date = date.AddDays(1))
            {
                if (!AttendanceCalendarViewModel.IsWorkingDay(date, holidays))
                    continue;

                decimal dayUnits = 1m;
                if (date == requestStart && request.IsHalfDayStart)
                    dayUnits -= 0.5m;
                if (date == requestEnd && request.IsHalfDayEnd)
                    dayUnits -= 0.5m;

                totalLeaveDays += Math.Max(dayUnits, 0m);
            }
        }

        return totalLeaveDays;
    }

    private async Task<UpcomingBirthdaysDashboardData> GetUpcomingBirthdaysAsync(int reminderWindowDays)
    {
        var today = DateOnly.FromDateTime(DateTimeHelper.VietnamToday);

        var employees = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active && e.DateOfBirth.HasValue)
            .Select(e => new
            {
                e.EmployeeId,
                e.EmployeeCode,
                e.FullName,
                e.DateOfBirth,
                DepartmentName = e.Department != null ? e.Department.DepartmentName : null
            })
            .ToListAsync();

        var items = employees
            .Select(e =>
            {
                var birthday = BirthdayCelebrationHelper.GetNextBirthdayDate(e.DateOfBirth!.Value, today);
                var daysUntilBirthday = birthday.DayNumber - today.DayNumber;

                if (daysUntilBirthday < 0 || daysUntilBirthday > reminderWindowDays)
                    return null;

                return new UpcomingBirthdayReminderItem
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.DepartmentName,
                    UpcomingBirthday = birthday,
                    DaysUntilBirthday = daysUntilBirthday,
                    UpcomingAge = Math.Max(1, birthday.Year - e.DateOfBirth.Value.Year)
                };
            })
            .Where(item => item != null)
            .Select(item => item!)
            .OrderBy(item => item.DaysUntilBirthday)
            .ThenBy(item => item.UpcomingBirthday)
            .ThenBy(item => item.FullName)
            .ToList();

        return new UpcomingBirthdaysDashboardData
        {
            ReminderWindowDays = reminderWindowDays,
            Items = items
        };
    }
}
