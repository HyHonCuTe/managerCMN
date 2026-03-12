using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class LeaveService : ILeaveService
{
    private readonly IUnitOfWork _unitOfWork;

    public LeaveService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<LeaveBalance?> GetBalanceAsync(int employeeId, int year)
        => await _unitOfWork.LeaveBalances.GetByEmployeeAndYearAsync(employeeId, year);

    public async Task<IEnumerable<LeaveRequest>> GetRequestsByEmployeeAsync(int employeeId)
        => await _unitOfWork.LeaveRequests.GetByEmployeeAsync(employeeId);

    public async Task<IEnumerable<LeaveRequest>> GetPendingRequestsAsync()
        => await _unitOfWork.LeaveRequests.GetPendingRequestsAsync();

    public async Task<LeaveRequest?> GetRequestByIdAsync(int requestId)
        => await _unitOfWork.LeaveRequests.GetByIdAsync(requestId);

    public async Task CreateRequestAsync(LeaveRequest request)
    {
        var balance = await _unitOfWork.LeaveBalances
            .GetByEmployeeAndYearAsync(request.EmployeeId, request.StartDate.Year);

        // Auto-determine pay type based on remaining leave
        if (balance != null && balance.RemainingLeave >= request.TotalDays)
        {
            request.PayType = LeavePayType.Paid;
            // Deduct leave immediately
            balance.UsedLeave += request.TotalDays;
            balance.RemainingLeave -= request.TotalDays;
            request.IsLeaveDeducted = true;
            _unitOfWork.LeaveBalances.Update(balance);
        }
        else
        {
            request.PayType = LeavePayType.Unpaid;
            request.IsLeaveDeducted = false;
        }

        await _unitOfWork.LeaveRequests.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ApproveRequestAsync(int requestId, int approverId)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId);
        if (request == null) return;

        request.Status = request.Status == RequestStatus.Pending
            ? RequestStatus.ManagerApproved
            : RequestStatus.HRApproved;
        request.ApprovedBy = approverId;
        request.ApprovedDate = DateTime.UtcNow;

        _unitOfWork.LeaveRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RejectRequestAsync(int requestId, int approverId)
    {
        var request = await _unitOfWork.LeaveRequests.GetByIdAsync(requestId);
        if (request == null) return;

        // Refund leave if it was deducted
        if (request.IsLeaveDeducted && request.PayType == LeavePayType.Paid)
        {
            var balance = await _unitOfWork.LeaveBalances
                .GetByEmployeeAndYearAsync(request.EmployeeId, request.StartDate.Year);
            if (balance != null)
            {
                balance.UsedLeave -= request.TotalDays;
                balance.RemainingLeave += request.TotalDays;
                _unitOfWork.LeaveBalances.Update(balance);
            }
            request.IsLeaveDeducted = false;
        }

        request.Status = RequestStatus.Rejected;
        request.ApprovedBy = approverId;
        request.ApprovedDate = DateTime.UtcNow;

        _unitOfWork.LeaveRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Allocate 3 days per quarter on the 26th of the first month of each quarter.
    /// Q1=Jan 26, Q2=Apr 26, Q3=Jul 26, Q4=Oct 26
    /// Also adds seniority bonus: 5yr=+1, 10yr=+2, 15yr=+3
    /// </summary>
    public async Task AllocateQuarterlyLeaveAsync()
    {
        var today = DateTime.UtcNow;
        var employees = await _unitOfWork.Employees.FindAsync(e => e.Status == EmployeeStatus.Active);

        foreach (var emp in employees)
        {
            var year = today.Year;
            var balance = await _unitOfWork.LeaveBalances.GetByEmployeeAndYearAsync(emp.EmployeeId, year);

            if (balance == null)
            {
                // Calculate seniority bonus
                decimal seniorityBonus = 0;
                if (emp.StartWorkingDate.HasValue)
                {
                    var yearsWorked = (today - emp.StartWorkingDate.Value).Days / 365;
                    if (yearsWorked >= 15) seniorityBonus = 3;
                    else if (yearsWorked >= 10) seniorityBonus = 2;
                    else if (yearsWorked >= 5) seniorityBonus = 1;
                }

                balance = new LeaveBalance
                {
                    EmployeeId = emp.EmployeeId,
                    Year = year,
                    TotalLeave = 3 + seniorityBonus,
                    UsedLeave = 0,
                    RemainingLeave = 3 + seniorityBonus,
                    CarryForward = 0
                };
                await _unitOfWork.LeaveBalances.AddAsync(balance);
            }
            else
            {
                // Add quarterly allocation (3 days)
                balance.TotalLeave += 3;
                balance.RemainingLeave += 3;
                _unitOfWork.LeaveBalances.Update(balance);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// Carry forward unused leave from previous year.
    /// Carried forward leave is valid until March 25 of the next year.
    /// After March 25, all carried forward leave is removed.
    /// </summary>
    public async Task ProcessCarryForwardAsync()
    {
        var today = DateTime.UtcNow;
        var currentYear = today.Year;
        var previousYear = currentYear - 1;

        // If after March 25, clear all carry-forward from previous year
        if (today.Month >= 3 && today.Day > 25)
        {
            var currentBalances = await _unitOfWork.LeaveBalances
                .FindAsync(lb => lb.Year == currentYear && lb.CarryForward > 0);

            foreach (var balance in currentBalances)
            {
                balance.RemainingLeave -= balance.CarryForward;
                if (balance.RemainingLeave < 0) balance.RemainingLeave = 0;
                balance.TotalLeave -= balance.CarryForward;
                balance.CarryForward = 0;
                _unitOfWork.LeaveBalances.Update(balance);
            }
        }
        else
        {
            // Carry forward remaining leave from previous year
            var previousBalances = await _unitOfWork.LeaveBalances
                .FindAsync(lb => lb.Year == previousYear && lb.RemainingLeave > 0);

            foreach (var prevBalance in previousBalances)
            {
                var currentBalance = await _unitOfWork.LeaveBalances
                    .GetByEmployeeAndYearAsync(prevBalance.EmployeeId, currentYear);

                if (currentBalance == null)
                {
                    currentBalance = new LeaveBalance
                    {
                        EmployeeId = prevBalance.EmployeeId,
                        Year = currentYear,
                        TotalLeave = prevBalance.RemainingLeave,
                        UsedLeave = 0,
                        RemainingLeave = prevBalance.RemainingLeave,
                        CarryForward = prevBalance.RemainingLeave
                    };
                    await _unitOfWork.LeaveBalances.AddAsync(currentBalance);
                }
                else
                {
                    currentBalance.CarryForward = prevBalance.RemainingLeave;
                    currentBalance.TotalLeave += prevBalance.RemainingLeave;
                    currentBalance.RemainingLeave += prevBalance.RemainingLeave;
                    _unitOfWork.LeaveBalances.Update(currentBalance);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
