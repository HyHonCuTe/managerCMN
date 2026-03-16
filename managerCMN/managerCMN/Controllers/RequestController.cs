using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class RequestController : Controller
{
    private readonly IRequestService _requestService;
    private readonly ILeaveService _leaveService;
    private readonly IWebHostEnvironment _env;

    public RequestController(IRequestService requestService, ILeaveService leaveService, IWebHostEnvironment env)
    {
        _requestService = requestService;
        _leaveService = leaveService;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var employeeId = GetCurrentEmployeeId();
        IEnumerable<Request> requests;

        if (IsPrivileged())
        {
            requests = await _requestService.FilterAsync(null, null);
            ViewBag.IsPrivileged = true;
        }
        else
        {
            requests = await _requestService.GetByEmployeeAsync(employeeId);
            if (employeeId > 0)
                ViewBag.LeaveSummary = await _leaveService.GetBalanceSummaryAsync(employeeId);
            ViewBag.IsPrivileged = false;
        }

        return View(requests);
    }

    public async Task<IActionResult> Create(RequestType? type)
    {
        var employeeId = GetCurrentEmployeeId();
        var model = new RequestCreateViewModel
        {
            RequestType = type ?? RequestType.Leave
        };

        await PopulateCreateViewModel(model, employeeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequestCreateViewModel model)
    {
        var employeeId = GetCurrentEmployeeId();

        if (!model.Approver1Id.HasValue || model.Approver1Id == 0)
        {
            ModelState.AddModelError("Approver1Id", "Không tìm thấy người duyệt 1. Vui lòng liên hệ quản trị.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateViewModel(model, employeeId);
            return View(model);
        }

        // Build reason text from LeaveReason
        if (model.LeaveReason.HasValue)
        {
            model.Reason = LeaveReasonHelper.GetDisplayName(model.LeaveReason.Value);
        }

        // Map half-day option: 0=Full, 1=Morning half, 2=Afternoon half
        model.IsHalfDayStart = model.HalfDayStartOption > 0;
        model.IsHalfDayEnd = model.HalfDayEndOption > 0;

        var request = new Request
        {
            EmployeeId = employeeId,
            RequestType = model.RequestType,
            Title = model.Title,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            IsHalfDayStart = model.IsHalfDayStart,
            IsHalfDayEnd = model.IsHalfDayEnd,
            IsHalfDayStartMorning = model.HalfDayStartOption == 1,
            IsHalfDayEndMorning = model.HalfDayEndOption == 1,
            LeaveReason = model.LeaveReason,
            Reason = model.Reason,
            Description = model.Description,
            TotalDays = _requestService.CalculateTotalDays(model.StartTime, model.EndTime, model.IsHalfDayStart, model.IsHalfDayEnd)
        };

        // Handle attachments
        if (model.Attachments?.Count > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "requests");
            Directory.CreateDirectory(uploadsDir);

            foreach (var file in model.Attachments)
            {
                if (file.Length == 0) continue;
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                request.Attachments.Add(new RequestAttachment
                {
                    FileName = file.FileName,
                    FilePath = $"/uploads/requests/{fileName}"
                });
            }
        }

        await _requestService.CreateAsync(request, model.Approver1Id!.Value, model.Approver2Id);
        TempData["Success"] = "Đã gửi đơn thành công!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await _requestService.GetWithDetailsAsync(id);
        if (request == null) return NotFound();

        var currentEmployeeId = GetCurrentEmployeeId();
        if (!IsPrivileged() && request.EmployeeId != currentEmployeeId)
            return Forbid();
        var pendingApproval = request.Approvals
            .FirstOrDefault(a => a.ApproverId == currentEmployeeId && a.Status == ApprovalStatus.Pending);

        bool isAdminOrMgr = User.IsInRole("Admin") || User.IsInRole("Manager");
        var model = new RequestDetailViewModel
        {
            Request = request,
            CanApprove = (isAdminOrMgr ? request.Status != RequestStatus.FullyApproved : pendingApproval != null)
                && request.Status != RequestStatus.Rejected
                && request.Status != RequestStatus.Cancelled,
            CurrentApproverOrder = pendingApproval?.ApproverOrder,
            IsOwner = request.EmployeeId == currentEmployeeId
        };

        return View(model);
    }

    public async Task<IActionResult> Approve(RequestStatus? status, RequestType? type, DateTime? from, DateTime? to)
    {
        if (!IsPrivileged())
            return Forbid();

        var employeeId = GetCurrentEmployeeId();
        bool isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");
        IEnumerable<Request> requests;

        if (isAdminOrManager)
        {
            requests = await _requestService.FilterAsync(status, type);
        }
        else
        {
            // IsApprover: only their assigned requests
            if (status.HasValue || type.HasValue)
            {
                var filtered = await _requestService.FilterAsync(status, type);
                requests = filtered.Where(r => r.Approvals.Any(a => a.ApproverId == employeeId));
            }
            else
            {
                requests = await _requestService.GetAllForApproverAsync(employeeId);
            }
        }

        if (from.HasValue)
            requests = requests.Where(r => r.CreatedDate >= from.Value);
        if (to.HasValue)
            requests = requests.Where(r => r.CreatedDate <= to.Value.AddDays(1));

        var model = new PendingApprovalsViewModel
        {
            Requests = requests.ToList(),
            FilterStatus = status,
            FilterType = type,
            FilterDateFrom = from,
            FilterDateTo = to,
            CurrentEmployeeId = employeeId
        };

        ViewBag.IsAdminOrManager = isAdminOrManager;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoApprove(RequestApprovalViewModel model)
    {
        if (!IsPrivileged()) return Forbid();
        var employeeId = GetCurrentEmployeeId();
        if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            await _requestService.ForceApproveAsync(model.RequestId, employeeId, model.Comment);
        else
            await _requestService.ApproveAsync(model.RequestId, employeeId, model.Comment);
        TempData["Success"] = "Đã duyệt đơn thành công!";
        return RedirectToAction(nameof(Approve));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoReject(RequestApprovalViewModel model)
    {
        if (!IsPrivileged()) return Forbid();
        var employeeId = GetCurrentEmployeeId();
        if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            await _requestService.ForceRejectAsync(model.RequestId, employeeId, model.Comment);
        else
            await _requestService.RejectAsync(model.RequestId, employeeId, model.Comment);
        TempData["Success"] = "Đã từ chối đơn!";
        return RedirectToAction(nameof(Approve));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        await _requestService.CancelAsync(id, employeeId);
        TempData["Success"] = "Đã hủy đơn!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employeeId = GetCurrentEmployeeId();
        var request = await _requestService.GetWithDetailsAsync(id);
        if (request == null) return NotFound();
        if (request.EmployeeId != employeeId || request.Status != RequestStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể chỉnh sửa đơn đang chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        var model = new RequestCreateViewModel
        {
            RequestId = request.RequestId,
            RequestType = request.RequestType,
            Title = request.Title,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsHalfDayStart = request.IsHalfDayStart,
            IsHalfDayEnd = request.IsHalfDayEnd,
            HalfDayStartOption = request.IsHalfDayStart ? (request.IsHalfDayStartMorning ? 1 : 2) : 0,
            HalfDayEndOption = request.IsHalfDayEnd ? (request.IsHalfDayEndMorning ? 1 : 2) : 0,
            LeaveReason = request.LeaveReason,
            Reason = request.Reason,
            Description = request.Description,
            TotalDays = request.TotalDays
        };

        await PopulateCreateViewModel(model, employeeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RequestCreateViewModel model)
    {
        var employeeId = GetCurrentEmployeeId();
        var request = await _requestService.GetWithDetailsAsync(id);
        if (request == null) return NotFound();
        if (request.EmployeeId != employeeId || request.Status != RequestStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể chỉnh sửa đơn đang chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        if (!model.Approver1Id.HasValue || model.Approver1Id == 0)
            ModelState.AddModelError("Approver1Id", "Không tìm thấy người duyệt 1.");

        if (!ModelState.IsValid)
        {
            model.RequestId = id;
            await PopulateCreateViewModel(model, employeeId);
            return View(model);
        }

        if (model.LeaveReason.HasValue)
            model.Reason = LeaveReasonHelper.GetDisplayName(model.LeaveReason.Value);

        model.IsHalfDayStart = model.HalfDayStartOption > 0;
        model.IsHalfDayEnd = model.HalfDayEndOption > 0;

        request.RequestType = model.RequestType;
        request.Title = model.Title;
        request.StartTime = model.StartTime;
        request.EndTime = model.EndTime;
        request.IsHalfDayStart = model.IsHalfDayStart;
        request.IsHalfDayEnd = model.IsHalfDayEnd;
        request.IsHalfDayStartMorning = model.HalfDayStartOption == 1;
        request.IsHalfDayEndMorning = model.HalfDayEndOption == 1;
        request.LeaveReason = model.LeaveReason;
        request.Reason = model.Reason;
        request.Description = model.Description;
        request.TotalDays = _requestService.CalculateTotalDays(model.StartTime, model.EndTime, model.IsHalfDayStart, model.IsHalfDayEnd);

        if (model.LeaveReason.HasValue)
            request.CountsAsWork = LeaveReasonHelper.GetCountsAsWork(model.LeaveReason.Value);

        await _requestService.UpdateAsync(request);
        TempData["Success"] = "Đã cập nhật đơn thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult GetReasons(RequestType type)
    {
        var reasons = LeaveReasonHelper.GetReasonsForType(type)
            .Select(r => new
            {
                value = (int)r.Reason,
                text = r.DisplayName,
                countsAsWork = r.CountsAsWork
            });
        return Json(reasons);
    }

    private async Task PopulateCreateViewModel(RequestCreateViewModel model, int employeeId)
    {
        // Auto-fill Approver 1 (department manager)
        var approver1Id = await _requestService.GetDefaultApprover1Async(employeeId);
        if (approver1Id.HasValue)
        {
            model.Approver1Id = approver1Id;
            var approver1 = await _requestService.GetByEmployeeIdAsync(approver1Id.Value);
            model.Approver1Name = approver1?.FullName ?? "Trưởng phòng";
        }

        // Approver 2 dropdown (from admin-configured IsApprover list)
        var availableApprovers = await _requestService.GetAvailableApprover2ListAsync();
        model.AvailableApprovers = availableApprovers
            .Where(a => a.EmployeeId != employeeId)
            .Select(a => new SelectListItem(a.FullName, a.EmployeeId.ToString()))
            .ToList();

        // Reasons for current type
        model.AvailableReasons = LeaveReasonHelper.GetReasonsForType(model.RequestType)
            .Select(r => new SelectListItem($"{r.DisplayName} → {(r.CountsAsWork ? "Tính công" : "Không tính công")}", ((int)r.Reason).ToString()))
            .ToList();
    }

    private bool IsPrivileged()
        => User.IsInRole("Admin") || User.IsInRole("Manager") || User.HasClaim("IsApprover", "true");

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
