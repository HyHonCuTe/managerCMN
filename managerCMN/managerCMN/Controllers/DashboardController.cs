using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILeaveService _leaveService;
    private readonly ApplicationDbContext _db;

    public DashboardController(IDashboardService dashboardService, ILeaveService leaveService, ApplicationDbContext db)
    {
        _dashboardService = dashboardService;
        _leaveService = leaveService;
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
                    var today = DateTime.Today;
                    var currentContract = employee.Contracts
                        .Where(c => c.Status == ContractStatus.Active || c.Status == ContractStatus.Renewed)
                        .OrderByDescending(c => c.StartDate)
                        .FirstOrDefault();

                    var monthStart = new DateOnly(today.Year, today.Month, 1);
                    var monthEnd   = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
                    var attendances = await _db.Set<Models.Entities.Attendance>()
                        .Where(a => a.EmployeeId == empId && a.Date >= monthStart && a.Date <= monthEnd)
                        .ToListAsync();

                    var pendingCount = await _db.Set<Models.Entities.Request>()
                        .CountAsync(r => r.EmployeeId == empId && r.Status == RequestStatus.Pending);

                    model.Personal = new PersonalDashboardData
                    {
                        FullName                = employee.FullName,
                        PositionName            = employee.Position?.PositionName,
                        DepartmentName          = employee.Department?.DepartmentName,
                        CurrentContract         = currentContract,
                        LeaveSummary            = await _leaveService.GetBalanceSummaryAsync(empId),
                        AttendanceDaysWorked    = attendances.Count(a => a.WorkingHours > 0),
                        AttendanceLateCount     = attendances.Count(a => a.IsLate),
                        AttendanceOvertimeHours = attendances.Sum(a => a.OvertimeHours ?? 0),
                        MyPendingRequests       = pendingCount
                    };
                }
            }
        }

        return View(model);
    }
}
