using System.Security.Claims;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class LeaveService : ILeaveService
{
    private const decimal QuarterlyLeaveDays = 3m;
    private const decimal AnnualLeaveDays = 12m;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LeaveService(IUnitOfWork unitOfWork, ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public async Task<LeaveBalance?> GetBalanceAsync(int employeeId, int year)
    {
        var asOfDate = Today();
        await EnsureBalanceForYearAsync(employeeId, year, asOfDate);
        return await _unitOfWork.LeaveBalances.GetByEmployeeAndYearAsync(employeeId, year);
    }

    public async Task<LeaveBalanceSummaryViewModel> GetBalanceSummaryAsync(int employeeId, DateTime? asOfDate = null)
    {
        var effectiveDate = NormalizeDate(asOfDate);
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên để tính phép.");
        var balance = await EnsureBalanceForYearAsync(employeeId, effectiveDate.Year, effectiveDate);
        var requests = await _unitOfWork.LeaveRequests
            .FindAsync(r => r.EmployeeId == employeeId && r.StartDate.Year == effectiveDate.Year);

        var paidLeaveTaken = requests
            .Where(r => r.PayType == LeavePayType.Paid && r.Status != RequestStatus.Rejected)
            .Sum(r => r.TotalDays);

        var unpaidLeaveTaken = requests
            .Where(r => r.PayType == LeavePayType.Unpaid && r.Status != RequestStatus.Rejected)
            .Sum(r => r.TotalDays);

        var currentYearRemaining = Math.Max(balance.TotalLeave - balance.UsedLeave, 0m);
        return new LeaveBalanceSummaryViewModel
        {
            EmployeeId = employeeId,
            Year = effectiveDate.Year,
            AsOfDate = effectiveDate,
            AnnualEntitlement = AnnualLeaveDays,
            GrantedQuarters = GetGrantedQuarterCount(employee, effectiveDate.Year, effectiveDate),
            CurrentYearAllocated = balance.TotalLeave,
            CurrentYearUsed = balance.UsedLeave,
            CurrentYearRemaining = currentYearRemaining,
            CarryForwardRemaining = balance.CarryForward,
            TotalRemaining = currentYearRemaining + balance.CarryForward,
            PaidLeaveTaken = paidLeaveTaken,
            UnpaidLeaveTaken = unpaidLeaveTaken,
            CarryForwardExpiryDate = GetCarryForwardExpiryDate(effectiveDate.Year)
        };
    }

    public async Task<IReadOnlyDictionary<int, LeaveBalanceSummaryViewModel>> GetBalanceSummariesAsync(IEnumerable<int> employeeIds, DateTime? asOfDate = null)
    {
        var result = new Dictionary<int, LeaveBalanceSummaryViewModel>();
        foreach (var employeeId in employeeIds.Distinct())
        {
            result[employeeId] = await GetBalanceSummaryAsync(employeeId, asOfDate);
        }

        return result;
    }

    public async Task<IEnumerable<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId)
        => await _unitOfWork.LeaveRequests.GetByEmployeeAsync(employeeId);

    public async Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync()
        => await _unitOfWork.LeaveRequests.GetPendingRequestsAsync();

    public async Task<LeaveRequest?> GetRequestByIdAsync(int requestId)
        => await _unitOfWork.LeaveRequests.GetByIdAsync(requestId);

    public async Task CreateRequestAsync(LeaveRequest request)
    {
        var balance = await EnsureBalanceForYearAsync(request.EmployeeId, request.StartDate.Year, request.StartDate);
        var currentYearRemaining = Math.Max(balance.TotalLeave - balance.UsedLeave, 0m);
        var carryForwardRemaining = IsCarryForwardWindowOpen(request.StartDate) ? balance.CarryForward : 0m;
        var availableLeave = currentYearRemaining + carryForwardRemaining;

        // Only override PayType if not already set to Unpaid (from CountsAsWork=false)
        if (request.PayType != LeavePayType.Unpaid)
            request.PayType = availableLeave >= request.TotalDays ? LeavePayType.Paid : LeavePayType.Unpaid;

        // Initialize deduction tracking - deduction will be handled by caller if needed
        request.IsLeaveDeducted = false;
        request.DeductedFromCarryForward = 0m;
        request.DeductedFromCurrentYear = 0m;

        await _unitOfWork.LeaveRequests.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo đơn nghỉ phép",
            "LeaveRequest",
            null,
            new { request.RequestId, request.EmployeeId, request.StartDate, request.EndDate, request.TotalDays, request.PayType },
            GetClientIP()
        );
    }

    public async Task ApproveRequestAsync(int requestId, int approverId)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId);
        if (request == null) return;

        var dataBefore = new { request.RequestId, request.EmployeeId, request.Status, request.ApprovedBy };

        request.Status = request.Status == RequestStatus.Pending
            ? RequestStatus.Approver1Approved
            : RequestStatus.FullyApproved;
        request.ApprovedBy = approverId;
        request.ApprovedDate = DateTime.UtcNow;

        _unitOfWork.LeaveRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Duyệt đơn nghỉ phép",
            "LeaveRequest",
            dataBefore,
            new { request.RequestId, request.EmployeeId, request.Status, request.ApprovedBy },
            GetClientIP()
        );
    }

    public async Task RejectRequestAsync(int requestId, int approverId)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId);
        if (request == null) return;

        var dataBefore = new { request.RequestId, request.EmployeeId, request.Status, request.ApprovedBy };

        if (request.IsLeaveDeducted && request.PayType == LeavePayType.Paid)
        {
            var balance = await EnsureBalanceForYearAsync(request.EmployeeId, request.StartDate.Year, Today());
            if (balance != null)
            {
                balance.UsedLeave = Math.Max(balance.UsedLeave - request.DeductedFromCurrentYear, 0m);
                if (request.StartDate.Year == Today().Year && IsCarryForwardWindowOpen(Today()))
                {
                    balance.CarryForward += request.DeductedFromCarryForward;
                }

                UpdateRemainingLeave(balance);
                _unitOfWork.LeaveBalances.Update(balance);
            }

            request.DeductedFromCurrentYear = 0m;
            request.DeductedFromCarryForward = 0m;
            request.IsLeaveDeducted = false;
        }

        request.Status = RequestStatus.Rejected;
        request.ApprovedBy = approverId;
        request.ApprovedDate = DateTime.UtcNow;

        _unitOfWork.LeaveRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Từ chối đơn nghỉ phép",
            "LeaveRequest",
            dataBefore,
            new { request.RequestId, request.EmployeeId, request.Status, request.ApprovedBy },
            GetClientIP()
        );
    }

    public async Task<bool> DeductLeaveForApprovedRequestAsync(int requestId)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId);
        if (request == null || request.IsLeaveDeducted || request.PayType != LeavePayType.Paid)
            return false;

        var balance = await EnsureBalanceForYearAsync(request.EmployeeId, request.StartDate.Year, request.StartDate);
        var currentYearRemaining = Math.Max(balance.TotalLeave - balance.UsedLeave, 0m);
        var carryForwardRemaining = IsCarryForwardWindowOpen(request.StartDate) ? balance.CarryForward : 0m;
        var availableLeave = currentYearRemaining + carryForwardRemaining;

        if (availableLeave < request.TotalDays)
        {
            // Insufficient balance at approval time - convert to unpaid
            request.PayType = LeavePayType.Unpaid;
            _unitOfWork.LeaveRequests.Update(request);
            await _unitOfWork.SaveChangesAsync();
            return false;
        }

        // Apply actual deduction
        var carryForwardUsed = Math.Min(carryForwardRemaining, request.TotalDays);
        var currentYearUsed = request.TotalDays - carryForwardUsed;

        balance.CarryForward -= carryForwardUsed;
        balance.UsedLeave += currentYearUsed;
        UpdateRemainingLeave(balance);

        request.IsLeaveDeducted = true;
        request.DeductedFromCarryForward = carryForwardUsed;
        request.DeductedFromCurrentYear = currentYearUsed;

        _unitOfWork.LeaveBalances.Update(balance);
        _unitOfWork.LeaveRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Trừ phép cho đơn đã duyệt",
            "LeaveRequest",
            null,
            new { request.RequestId, request.EmployeeId, CarryForwardUsed = carryForwardUsed, CurrentYearUsed = currentYearUsed },
            GetClientIP()
        );

        return true;
    }

    public async Task AdjustBalanceAsync(int employeeId, int year, decimal currentYearAdjustment, decimal carryForwardAdjustment)
    {
        Console.WriteLine($"=== LeaveService.AdjustBalanceAsync START ===");
        Console.WriteLine($"Input: EmployeeId={employeeId}, Year={year}, CurrentAdjustment={currentYearAdjustment}, CarryForwardAdjustment={carryForwardAdjustment}");

        var balance = await EnsureBalanceForYearAsync(employeeId, year, Today());
        Console.WriteLine($"Retrieved balance: ID={balance.LeaveId}, EmployeeId={balance.EmployeeId}, Year={balance.Year}");
        Console.WriteLine($"Before adjustment: TotalLeave={balance.TotalLeave}, UsedLeave={balance.UsedLeave}, CarryForward={balance.CarryForward}, RemainingLeave={balance.RemainingLeave}");

        var newTotalLeave = balance.TotalLeave + currentYearAdjustment;
        var newCarryForward = balance.CarryForward + carryForwardAdjustment;
        Console.WriteLine($"Calculated new values: TotalLeave={newTotalLeave}, CarryForward={newCarryForward}");

        if (newTotalLeave < balance.UsedLeave)
        {
            Console.WriteLine($"VALIDATION ERROR: newTotalLeave ({newTotalLeave}) < UsedLeave ({balance.UsedLeave})");
            throw new InvalidOperationException("Không thể giảm phép năm xuống thấp hơn số phép đã sử dụng.");
        }

        if (newCarryForward < 0)
        {
            Console.WriteLine($"VALIDATION ERROR: newCarryForward ({newCarryForward}) < 0");
            throw new InvalidOperationException("Không thể giảm phép bảo lưu xuống nhỏ hơn 0.");
        }

        // Apply changes
        Console.WriteLine($"Applying changes to balance object...");
        balance.TotalLeave = newTotalLeave;
        balance.CarryForward = newCarryForward;
        balance.IsManuallyAdjusted = true; // Mark as manually adjusted to prevent auto-recalculation

        Console.WriteLine($"Before UpdateRemainingLeave: TotalLeave={balance.TotalLeave}, UsedLeave={balance.UsedLeave}, CarryForward={balance.CarryForward}, RemainingLeave={balance.RemainingLeave}");
        UpdateRemainingLeave(balance);
        Console.WriteLine($"After UpdateRemainingLeave: TotalLeave={balance.TotalLeave}, UsedLeave={balance.UsedLeave}, CarryForward={balance.CarryForward}, RemainingLeave={balance.RemainingLeave}");
        Console.WriteLine($"LastUpdated set to: {balance.LastUpdated}");

        Console.WriteLine($"Calling _unitOfWork.LeaveBalances.Update...");
        _unitOfWork.LeaveBalances.Update(balance);

        Console.WriteLine($"Calling _unitOfWork.SaveChangesAsync...");
        await _unitOfWork.SaveChangesAsync();
        Console.WriteLine($"SaveChangesAsync completed successfully");

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Điều chỉnh số dư phép",
            "LeaveBalance",
            new { EmployeeId = employeeId, Year = year },
            new { EmployeeId = employeeId, Year = year, CurrentYearAdjustment = currentYearAdjustment, CarryForwardAdjustment = carryForwardAdjustment, NewTotalLeave = balance.TotalLeave, NewCarryForward = balance.CarryForward },
            GetClientIP()
        );

        // Verify the save by re-querying
        Console.WriteLine($"Re-querying balance to verify save...");
        var verifyBalance = await _unitOfWork.LeaveBalances.GetByEmployeeAndYearAsync(employeeId, year);
        if (verifyBalance != null)
        {
            Console.WriteLine($"Verification query result: TotalLeave={verifyBalance.TotalLeave}, UsedLeave={verifyBalance.UsedLeave}, CarryForward={verifyBalance.CarryForward}, RemainingLeave={verifyBalance.RemainingLeave}, LastUpdated={verifyBalance.LastUpdated}");
        }
        else
        {
            Console.WriteLine($"WARNING: Verification query returned null!");
        }

        Console.WriteLine($"=== LeaveService.AdjustBalanceAsync END ===");
    }

    /// <summary>
    /// Allocate 3 days per quarter on the 26th of the first month of each quarter.
    /// Q1=Dec 26 (of previous year), Q2=Mar 26, Q3=Jun 26, Q4=Sep 26
    /// Seniority bonus is granted on March 26 annually: 5yr=+1, 10yr=+2, 15yr=+3, etc.
    /// Maximum carry-forward from previous year is 5 days (valid until March 25).
    /// </summary>
    public async Task AllocateQuarterlyLeaveAsync()
    {
        var today = Today();
        var employees = await _unitOfWork.Employees.FindAsync(e => e.Status == EmployeeStatus.Active);

        var processedCount = 0;
        foreach (var emp in employees)
        {
            await EnsureBalanceForYearAsync(emp.EmployeeId, today.Year, today);
            processedCount++;
        }

        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Phân bổ phép theo quý",
            "LeaveBalance",
            null,
            new { ProcessedEmployees = processedCount, Year = today.Year, ProcessDate = today },
            GetClientIP()
        );
    }

    /// <summary>
    /// Carry forward unused leave from previous year (maximum 5 days).
    /// Carried forward leave is valid until March 25 of the next year.
    /// After March 25, all carried forward leave expires and is removed.
    /// </summary>
    public async Task ProcessCarryForwardAsync()
    {
        var today = Today();
        var employees = await _unitOfWork.Employees.FindAsync(e => e.Status == EmployeeStatus.Active);

        var processedCount = 0;
        foreach (var employee in employees)
        {
            await EnsureBalanceForYearAsync(employee.EmployeeId, today.Year, today);
            processedCount++;
        }

        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Xử lý chuyển phép năm trước",
            "LeaveBalance",
            null,
            new { ProcessedEmployees = processedCount, Year = today.Year, ProcessDate = today },
            GetClientIP()
        );
    }

    private async Task<LeaveBalance> EnsureBalanceForYearAsync(int employeeId, int year, DateTime asOfDate)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên để tính phép.");

        var balance = await _unitOfWork.LeaveBalances.GetByEmployeeAndYearAsync(employeeId, year);
        if (balance == null)
        {
            balance = new LeaveBalance
            {
                EmployeeId = employeeId,
                Year = year,
                UsedLeave = 0m,
                CarryForward = GetInitialCarryForward(employeeId, year, asOfDate),
            };
            await _unitOfWork.LeaveBalances.AddAsync(balance);
        }

        var accruedCurrentYearLeave = GetAccruedCurrentYearLeave(employee, year, asOfDate);

        // Only auto-calculate TotalLeave if not manually adjusted
        if (!balance.IsManuallyAdjusted)
        {
            balance.TotalLeave = accruedCurrentYearLeave;
        }

        if (!IsCarryForwardWindowOpen(asOfDate) || asOfDate.Year != year)
        {
            balance.CarryForward = 0m;
        }

        UpdateRemainingLeave(balance);
        return balance;
    }

    private decimal GetInitialCarryForward(int employeeId, int year, DateTime asOfDate)
    {
        if (!IsCarryForwardWindowOpen(asOfDate))
        {
            return 0m;
        }

        var previousBalance = _unitOfWork.LeaveBalances
            .Query()
            .FirstOrDefault(lb => lb.EmployeeId == employeeId && lb.Year == year - 1);

        if (previousBalance == null)
        {
            return 0m;
        }

        // LIMIT: Maximum 5 days can be carried forward
        var carryForwardAmount = Math.Max(previousBalance.RemainingLeave, 0m);
        return Math.Min(carryForwardAmount, 5m);
    }

    private static decimal GetAccruedCurrentYearLeave(Employee employee, int year, DateTime asOfDate)
    {
        if (employee.StartWorkingDate == null)
        {
            return 0m;
        }

        var startDate = employee.StartWorkingDate.Value.Date;
        var grantDates = new[]
        {
            new DateTime(year - 1, 12, 26), // Q1 of current year
            new DateTime(year, 3, 26),      // Q2
            new DateTime(year, 6, 26),      // Q3
            new DateTime(year, 9, 26)       // Q4
        };

        // Calculate quarterly leave (3 days per quarter)
        var grantedQuarters = grantDates.Count(grantDate => asOfDate.Date >= grantDate && startDate <= grantDate);
        var quarterlyLeave = Math.Min(grantedQuarters * QuarterlyLeaveDays, AnnualLeaveDays);

        // Calculate seniority bonus (granted on March 26 annually)
        // 5 years = +1 day, 10 years = +2 days, 15 years = +3 days, etc.
        decimal seniorityBonus = 0m;
        var seniorityGrantDate = new DateTime(year, 3, 26);

        if (asOfDate.Date >= seniorityGrantDate)
        {
            var yearsOfService = (seniorityGrantDate - startDate).TotalDays / 365.25;
            seniorityBonus = Math.Floor((decimal)yearsOfService / 5m);
        }

        return quarterlyLeave + seniorityBonus;
    }

    private static int GetGrantedQuarterCount(Employee employee, int year, DateTime asOfDate)
    {
        if (employee.StartWorkingDate == null)
        {
            return 0;
        }

        var startDate = employee.StartWorkingDate.Value.Date;
        var grantDates = new[]
        {
            new DateTime(year - 1, 12, 26),
            new DateTime(year, 3, 26),
            new DateTime(year, 6, 26),
            new DateTime(year, 9, 26)
        };

        return grantDates.Count(grantDate => asOfDate.Date >= grantDate && startDate <= grantDate);
    }

    private static void UpdateRemainingLeave(LeaveBalance balance)
    {
        Console.WriteLine($"UpdateRemainingLeave START: TotalLeave={balance.TotalLeave}, UsedLeave={balance.UsedLeave}, CarryForward={balance.CarryForward}");

        var currentYearRemaining = Math.Max(balance.TotalLeave - balance.UsedLeave, 0m);
        Console.WriteLine($"Calculated currentYearRemaining: {currentYearRemaining}");

        var finalCarryForward = Math.Max(balance.CarryForward, 0m);
        Console.WriteLine($"Final CarryForward (after Math.Max): {finalCarryForward}");

        balance.RemainingLeave = currentYearRemaining + finalCarryForward;
        balance.LastUpdated = DateTime.UtcNow;

        Console.WriteLine($"UpdateRemainingLeave END: RemainingLeave={balance.RemainingLeave}, LastUpdated={balance.LastUpdated}");
    }

    private static bool IsCarryForwardWindowOpen(DateTime date)
        => date.Date <= GetCarryForwardExpiryDate(date.Year);

    private static DateTime GetCarryForwardExpiryDate(int year)
        => new(year, 3, 25);

    private static DateTime NormalizeDate(DateTime? date)
        => (date ?? Today()).Date;

    private static DateTime Today()
        => DateTimeHelper.VietnamToday;
}
