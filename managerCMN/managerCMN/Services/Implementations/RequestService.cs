using System.Security.Claims;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class RequestService : IRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILeaveService _leaveService;
    private readonly INotificationService _notificationService;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestService(IUnitOfWork unitOfWork, ILeaveService leaveService, INotificationService notificationService,
        ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _leaveService = leaveService;
        _notificationService = notificationService;
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public async Task<IEnumerable<Request>> GetAllAsync()
        => await _unitOfWork.Requests.GetAllAsync();

    public async Task<Request?> GetByIdAsync(int id)
        => await _unitOfWork.Requests.GetByIdAsync(id);

    public async Task<Request?> GetWithDetailsAsync(int id)
        => await _unitOfWork.Requests.GetWithApprovalsAsync(id);

    public async Task<Request?> GetWithAttachmentsAsync(int id)
        => await _unitOfWork.Requests.GetWithAttachmentsAsync(id);

    public async Task<IEnumerable<Request>> GetByEmployeeAsync(int employeeId)
        => await _unitOfWork.Requests.GetByEmployeeAsync(employeeId);

    public async Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status)
        => await _unitOfWork.Requests.GetByStatusAsync(status);

    public async Task<IEnumerable<Request>> GetPendingForApproverAsync(int approverEmployeeId)
        => await _unitOfWork.Requests.GetPendingForApproverAsync(approverEmployeeId);

    public async Task<IEnumerable<Request>> GetAllForApproverAsync(int approverEmployeeId)
        => await _unitOfWork.Requests.GetAllForApproverAsync(approverEmployeeId);

    public async Task<IEnumerable<Request>> FilterAsync(RequestStatus? status, RequestType? type)
        => await _unitOfWork.Requests.GetByStatusAndTypeAsync(status, type);

    public async Task CreateAsync(Request request, int approver1Id, int approver2Id)
    {
        request.Status = RequestStatus.Pending;
        request.CreatedDate = DateTime.UtcNow;

        if (request.LeaveReason.HasValue)
            request.CountsAsWork = LeaveReasonHelper.GetCountsAsWork(request.LeaveReason.Value);

        await _unitOfWork.Requests.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();

        // Create 2 approval records
        await _unitOfWork.RequestApprovals.AddAsync(new RequestApproval
        {
            RequestId = request.RequestId,
            ApproverId = approver1Id,
            ApproverOrder = 1,
            Status = ApprovalStatus.Pending
        });
        await _unitOfWork.RequestApprovals.AddAsync(new RequestApproval
        {
            RequestId = request.RequestId,
            ApproverId = approver2Id,
            ApproverOrder = 2,
            Status = ApprovalStatus.Pending
        });

        // If Leave type with paid leave, create LeaveRequest and deduct leave immediately
        if (request.RequestType == RequestType.Leave)
        {
            var leaveRequest = new LeaveRequest
            {
                EmployeeId = request.EmployeeId,
                StartDate = request.StartTime.Date,
                EndDate = request.EndTime.Date,
                TotalDays = request.TotalDays,
                Reason = request.Reason,
                PayType = request.CountsAsWork ? LeavePayType.Paid : LeavePayType.Unpaid
            };
            await _leaveService.CreateRequestAsync(leaveRequest);

            // Deduct leave immediately for paid leave requests
            if (leaveRequest.PayType == LeavePayType.Paid)
            {
                var deductionSuccess = await _leaveService.DeductLeaveForApprovedRequestAsync(leaveRequest.RequestId);
                if (!deductionSuccess)
                {
                    throw new InvalidOperationException("Không thể trừ phép - số dư không đủ hoặc có lỗi xảy ra.");
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify approvers
        var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId);
        var empName = employee?.FullName ?? "Nhân viên";
        var typeText = GetRequestTypeText(request.RequestType);
        await NotifyApprover(approver1Id, $"Đơn từ mới cần duyệt", $"{empName} đã gửi {typeText}: {request.Title}");
        await NotifyApprover(approver2Id, $"Đơn từ mới cần duyệt", $"{empName} đã gửi {typeText}: {request.Title}");

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo đơn từ",
            "Request",
            null,
            new { request.RequestId, request.Title, request.RequestType, request.EmployeeId, request.StartTime, request.EndTime, request.TotalDays },
            GetClientIP()
        );
    }

    public async Task UpdateAsync(Request request)
    {
        _unitOfWork.Requests.Update(request);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ApproveAsync(int requestId, int approverEmployeeId, string? comment = null)
    {
        var request = await _unitOfWork.Requests.GetWithApprovalsAsync(requestId);
        if (request == null) return;
        if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Cancelled) return;

        var approval = request.Approvals.FirstOrDefault(a =>
            a.ApproverId == approverEmployeeId && a.Status == ApprovalStatus.Pending);
        if (approval == null) return;

        approval.Status = ApprovalStatus.Approved;
        approval.ApprovedDate = DateTime.UtcNow;
        approval.Comment = comment;
        _unitOfWork.RequestApprovals.Update(approval);

        // Determine overall status
        var allApproved = request.Approvals.All(a => a.Status == ApprovalStatus.Approved);
        if (allApproved)
        {
            request.Status = RequestStatus.FullyApproved;
            // Note: Leave deduction is already handled at request creation for paid leave
        }
        else if (approval.ApproverOrder == 1)
        {
            request.Status = RequestStatus.Approver1Approved;
        }
        else
        {
            request.Status = RequestStatus.Approver2Approved;
        }

        _unitOfWork.Requests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        // Notify employee
        var approver = await _unitOfWork.Employees.GetByIdAsync(approverEmployeeId);
        var approverName = approver?.FullName ?? "Người duyệt";
        await NotifyEmployee(request.EmployeeId,
            "Đơn đã được duyệt",
            $"{approverName} đã duyệt đơn: {request.Title}" + (allApproved ? " (Đã duyệt hoàn tất)" : ""));

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            allApproved ? "Duyệt đơn hoàn tất" : "Duyệt đơn (cấp " + approval.ApproverOrder + ")",
            "Request",
            new { request.RequestId, StatusBefore = "Pending" },
            new { request.RequestId, request.Title, request.Status, ApprovedBy = approverName },
            GetClientIP()
        );
    }

    public async Task RejectAsync(int requestId, int approverEmployeeId, string? comment = null)
    {
        var request = await _unitOfWork.Requests.GetWithApprovalsAsync(requestId);
        if (request == null) return;

        var approval = request.Approvals.FirstOrDefault(a =>
            a.ApproverId == approverEmployeeId && a.Status == ApprovalStatus.Pending);
        if (approval == null) return;

        approval.Status = ApprovalStatus.Rejected;
        approval.ApprovedDate = DateTime.UtcNow;
        approval.Comment = comment;
        _unitOfWork.RequestApprovals.Update(approval);

        request.Status = RequestStatus.Rejected;
        _unitOfWork.Requests.Update(request);

        // If Leave type, reverse deduction
        if (request.RequestType == RequestType.Leave)
        {
            var startDate = request.StartTime.Date;
            var endDate = request.EndTime.Date;
            var leaveRequests = await _unitOfWork.LeaveRequests
                .FindAsync(lr => lr.EmployeeId == request.EmployeeId
                              && lr.StartDate.Date == startDate
                              && lr.EndDate.Date == endDate);
            var leaveReq = leaveRequests.FirstOrDefault();
            if (leaveReq != null)
            {
                await _leaveService.RejectRequestAsync(leaveReq.RequestId, approverEmployeeId);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify employee
        var approver = await _unitOfWork.Employees.GetByIdAsync(approverEmployeeId);
        var approverName = approver?.FullName ?? "Người duyệt";
        await NotifyEmployee(request.EmployeeId,
            "Đơn bị từ chối",
            $"{approverName} đã từ chối đơn: {request.Title}" + (!string.IsNullOrEmpty(comment) ? $". Lý do: {comment}" : ""));

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Từ chối đơn",
            "Request",
            new { request.RequestId, StatusBefore = "Pending" },
            new { request.RequestId, request.Title, request.Status, RejectedBy = approverName, Comment = comment },
            GetClientIP()
        );
    }

    public async Task ForceApproveAsync(int requestId, int adminEmployeeId, string? comment = null)
    {
        var request = await _unitOfWork.Requests.GetWithApprovalsAsync(requestId);
        if (request == null) return;
        if (request.Status == RequestStatus.FullyApproved
            || request.Status == RequestStatus.Rejected
            || request.Status == RequestStatus.Cancelled) return;

        foreach (var a in request.Approvals.Where(a => a.Status == ApprovalStatus.Pending))
        {
            a.Status = ApprovalStatus.Approved;
            a.ApprovedDate = DateTime.UtcNow;
            a.Comment = comment;
            _unitOfWork.RequestApprovals.Update(a);
        }
        request.Status = RequestStatus.FullyApproved;

        // Note: Leave deduction is already handled at request creation for paid leave

        _unitOfWork.Requests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        var approver = await _unitOfWork.Employees.GetByIdAsync(adminEmployeeId);
        var approverName = approver?.FullName ?? "Admin";
        await NotifyEmployee(request.EmployeeId, "Đơn đã được duyệt",
            $"{approverName} đã duyệt đơn: {request.Title} (Đã duyệt hoàn tất)");

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Admin duyệt đơn",
            "Request",
            new { request.RequestId, StatusBefore = "Pending" },
            new { request.RequestId, request.Title, request.Status, ApprovedBy = approverName },
            GetClientIP()
        );
    }

    public async Task ForceRejectAsync(int requestId, int adminEmployeeId, string? comment = null)
    {
        var request = await _unitOfWork.Requests.GetWithApprovalsAsync(requestId);
        if (request == null) return;
        if (request.Status == RequestStatus.Rejected || request.Status == RequestStatus.Cancelled) return;

        foreach (var a in request.Approvals.Where(a => a.Status == ApprovalStatus.Pending))
        {
            a.Status = ApprovalStatus.Rejected;
            a.ApprovedDate = DateTime.UtcNow;
            a.Comment = comment;
            _unitOfWork.RequestApprovals.Update(a);
        }
        request.Status = RequestStatus.Rejected;
        _unitOfWork.Requests.Update(request);

        if (request.RequestType == RequestType.Leave)
        {
            var startDate = request.StartTime.Date;
            var endDate = request.EndTime.Date;
            var leaveRequests = await _unitOfWork.LeaveRequests
                .FindAsync(lr => lr.EmployeeId == request.EmployeeId
                              && lr.StartDate.Date == startDate
                              && lr.EndDate.Date == endDate);
            var leaveReq = leaveRequests.FirstOrDefault();
            if (leaveReq != null)
                await _leaveService.RejectRequestAsync(leaveReq.RequestId, adminEmployeeId);
        }

        await _unitOfWork.SaveChangesAsync();

        var approver = await _unitOfWork.Employees.GetByIdAsync(adminEmployeeId);
        var approverName = approver?.FullName ?? "Admin";
        await NotifyEmployee(request.EmployeeId, "Đơn bị từ chối",
            $"{approverName} đã từ chối đơn: {request.Title}"
            + (!string.IsNullOrEmpty(comment) ? $". Lý do: {comment}" : ""));

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Admin từ chối đơn",
            "Request",
            new { request.RequestId, StatusBefore = "Pending" },
            new { request.RequestId, request.Title, request.Status, RejectedBy = approverName, Comment = comment },
            GetClientIP()
        );
    }

    public async Task RevertApprovalAsync(int requestId, int adminEmployeeId, string? comment = null)
    {
        var request = await _unitOfWork.Requests.GetWithApprovalsAsync(requestId);
        if (request == null) return;

        // Only allow reverting approved requests (any approval level)
        if (request.Status != RequestStatus.Approver1Approved
            && request.Status != RequestStatus.Approver2Approved
            && request.Status != RequestStatus.FullyApproved)
        {
            throw new InvalidOperationException("Chỉ có thể hoàn duyệt đơn đã được duyệt.");
        }

        // Mark all approvals as rejected with admin comment (for audit trail)
        foreach (var approval in request.Approvals)
        {
            approval.Status = ApprovalStatus.Rejected;
            approval.ApprovedDate = DateTime.UtcNow;
            approval.Comment = $"[HOÀN DUYỆT BỞI ADMIN] {comment}";
            _unitOfWork.RequestApprovals.Update(approval);
        }

        // Change request status to Cancelled (not Rejected)
        request.Status = RequestStatus.Cancelled;
        _unitOfWork.Requests.Update(request);

        // If Leave type, restore leave balance using existing rejection logic
        if (request.RequestType == RequestType.Leave)
        {
            var startDate = request.StartTime.Date;
            var endDate = request.EndTime.Date;
            var leaveRequests = await _unitOfWork.LeaveRequests
                .FindAsync(lr => lr.EmployeeId == request.EmployeeId
                              && lr.StartDate.Date == startDate
                              && lr.EndDate.Date == endDate);
            var leaveReq = leaveRequests.FirstOrDefault();
            if (leaveReq != null)
            {
                await _leaveService.RejectRequestAsync(leaveReq.RequestId, adminEmployeeId);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify employee
        var admin = await _unitOfWork.Employees.GetByIdAsync(adminEmployeeId);
        var adminName = admin?.FullName ?? "Admin";
        await NotifyEmployee(request.EmployeeId,
            "Đơn đã được hoàn duyệt",
            $"{adminName} đã hoàn duyệt đơn: {request.Title}"
            + (!string.IsNullOrEmpty(comment) ? $". Lý do: {comment}" : ""));

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Hoàn duyệt đơn",
            "Request",
            new { request.RequestId, StatusBefore = "Approved" },
            new { request.RequestId, request.Title, request.Status, RevertedBy = adminName, Comment = comment },
            GetClientIP()
        );
    }

    public async Task CancelAsync(int requestId, int employeeId)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null || request.EmployeeId != employeeId) return;
        if (request.Status != RequestStatus.Pending) return;

        request.Status = RequestStatus.Cancelled;
        _unitOfWork.Requests.Update(request);

        // If Leave type, reverse deduction
        if (request.RequestType == RequestType.Leave)
        {
            var leaveRequests = await _unitOfWork.LeaveRequests
                .FindAsync(lr => lr.EmployeeId == request.EmployeeId
                              && lr.StartDate == request.StartTime.Date
                              && lr.EndDate == request.EndTime.Date
                              && lr.Status != RequestStatus.Rejected
                              && lr.Status != RequestStatus.Cancelled);
            var leaveReq = leaveRequests.FirstOrDefault();
            if (leaveReq != null)
            {
                await _leaveService.RejectRequestAsync(leaveReq.RequestId, employeeId);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Hủy đơn",
            "Request",
            new { request.RequestId, StatusBefore = "Pending" },
            new { request.RequestId, request.Title, request.Status },
            GetClientIP()
        );
    }

    public async Task<int?> GetDefaultApprover1Async(int employeeId)
    {
        // This method is now deprecated in favor of GetDepartmentManagersAsync
        // Keep for backward compatibility but always return null to force manual selection
        return null;
    }

    public async Task<bool> NeedsManualApprover1SelectionAsync(int employeeId)
    {
        // All employees now need manual selection, but the source list differs
        return true;
    }

    public async Task<IEnumerable<Employee>> GetDepartmentManagersAsync(int employeeId)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(employeeId);
        if (employee?.DepartmentId == null)
        {
            return Enumerable.Empty<Employee>();
        }

        // Check if employee has low-level JobTitle (Nhân viên = 4, Thực tập = 5)
        var isLowLevelPosition = IsLowLevelJobTitle(employee.JobTitleId);

        if (isLowLevelPosition)
        {
            // Low-level: Get all "Trưởng phòng" (JobTitleId = 2) in the same department
            var allEmployees = await _unitOfWork.Employees.GetAllAsync();

            var departmentManagers = allEmployees
                .Where(e => e.DepartmentId == employee.DepartmentId &&
                           e.EmployeeId != employeeId &&
                           e.Status == EmployeeStatus.Active &&
                           e.JobTitleId == 2) // Trưởng phòng
                .OrderBy(e => e.FullName);

            // If no "Trưởng phòng" found, fallback to Approver2 list (return empty to trigger fallback)
            if (!departmentManagers.Any())
            {
                return Enumerable.Empty<Employee>();
            }

            return departmentManagers;
        }
        else
        {
            // High-level (Trưởng phòng, Ban Giám Đốc): Return empty (will use Approver2 list instead)
            return Enumerable.Empty<Employee>();
        }
    }

    /// <summary>
    /// Check if JobTitle is low-level (Nhân viên = 4, Thực tập = 5)
    /// </summary>
    private static bool IsLowLevelJobTitle(int? jobTitleId)
    {
        if (!jobTitleId.HasValue) return true; // No JobTitle = low level

        // Low-level JobTitles:
        // 4 = Nhân viên
        // 5 = Thực tập
        return jobTitleId == 4 || jobTitleId == 5;
    }

    public async Task<Employee?> GetByEmployeeIdAsync(int employeeId)
        => await _unitOfWork.Employees.GetByIdAsync(employeeId);

    public async Task<IEnumerable<Employee>> GetAvailableApprover2ListAsync()
    {
        var allEmployees = await _unitOfWork.Employees.GetAllAsync();
        return allEmployees
            .Where(e => e.IsApprover && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.FullName);
    }

    public async Task<IEnumerable<Employee>> GetAllDepartmentEmployeesAsync(int departmentId)
    {
        var allEmployees = await _unitOfWork.Employees.GetAllAsync();
        return allEmployees
            .Where(e => e.DepartmentId == departmentId && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.FullName);
    }

    public decimal CalculateTotalDays(DateTime start, DateTime end, bool halfDayStart, bool halfDayEnd)
    {
        var days = (end.Date - start.Date).Days + 1m;
        if (halfDayStart) days -= 0.5m;
        if (halfDayEnd) days -= 0.5m;
        return Math.Max(days, 0.5m);
    }

    private async Task NotifyApprover(int approverEmployeeId, string title, string message)
    {
        var user = (await _unitOfWork.Users.FindAsync(u => u.EmployeeId == approverEmployeeId)).FirstOrDefault();
        if (user != null)
            await _notificationService.CreateAsync(user.UserId, title, message);
    }

    private async Task NotifyEmployee(int employeeId, string title, string message)
    {
        var user = (await _unitOfWork.Users.FindAsync(u => u.EmployeeId == employeeId)).FirstOrDefault();
        if (user != null)
            await _notificationService.CreateAsync(user.UserId, title, message);
    }

    private static string GetRequestTypeText(RequestType type) => type switch
    {
        RequestType.Leave => "đơn xin nghỉ",
        RequestType.CheckInOut => "đơn checkin/out",
        RequestType.Absence => "đơn vắng mặt",
        RequestType.WorkFromHome => "đơn xin làm ở nhà",
        _ => "đơn từ"
    };
}
