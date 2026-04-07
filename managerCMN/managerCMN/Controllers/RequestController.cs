using System.ComponentModel.DataAnnotations;
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
        var isAdmin = User.IsInRole("Admin");

        if (isAdmin)
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
        var today = DateTimeHelper.VietnamToday;
        var model = new RequestCreateViewModel
        {
            RequestType = type ?? RequestType.Leave,
            StartDate = today,
            EndDate = today
        };

        await PopulateCreateViewModel(model, employeeId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequestCreateViewModel model)
    {
        var employeeId = GetCurrentEmployeeId();
        NormalizeRequiredTextFields(model);
        ValidateRequiredRequestFields(model);
        ApplyRequestDateInputs(model);
        NormalizeSingleDayRequest(model);
        ValidateTimeBasedRequest(model);

        // All employees now need to select Approver1 manually
        if (!HasValidationError(nameof(RequestCreateViewModel.Approver1Id))
            && (!model.Approver1Id.HasValue || model.Approver1Id == 0))
        {
            ModelState.AddModelError("Approver1Id", "Vui lòng chọn người duyệt 1.");
        }

        // Validate monthly request limits
        var today = DateTimeHelper.VietnamToday;
        
        // Check Absence limit: max 2 per month
        if (model.RequestType == RequestType.Absence)
        {
            var absenceCount = await _requestService.CountAbsenceRequestsInMonthAsync(employeeId, today);
            if (absenceCount >= 2)
            {
                ModelState.AddModelError("RequestType",
                    $"Bạn đã đạt giới hạn 2 đơn vắng mặt trong tháng này.");
            }
        }

        // Check CheckInOut limit: max 5 per month
        if (model.RequestType == RequestType.CheckInOut)
        {
            var checkInOutCount = await _requestService.CountCheckInOutRequestsInMonthAsync(employeeId, today);
            if (checkInOutCount >= 5)
            {
                ModelState.AddModelError("RequestType",
                    $"Bạn đã đạt giới hạn 5 đơn checkin/out trong tháng này.");
            }
        }

        // Validate request date: Cannot create request for past dates older than 5 working days
        var requestStartDate = model.StartTime.Date;

        if (!HasValidationError(nameof(RequestCreateViewModel.StartTime)) && requestStartDate < today)
        {
            var workingDaysBetween = DateTimeHelper.CountWorkingDaysBetween(requestStartDate, today);
            // workingDaysBetween includes both start and end dates, so we need to subtract 1
            var daysAgo = workingDaysBetween - 1;

            if (daysAgo > 5)
            {
                ModelState.AddModelError("StartTime",
                    $"Không thể tạo đơn cho ngày quá xa trong quá khứ. " +
                    $"Bạn chỉ có thể tạo đơn cho tối đa 5 ngày làm việc trước (không tính T7, CN). " +
                    $"Ngày bạn chọn ({requestStartDate:dd/MM/yyyy}) cách đây {daysAgo} ngày làm việc.");
            }
        }

        var isHalfDayStart = model.HalfDayStartOption > 0;
        var isHalfDayEnd = model.HalfDayEndOption > 0;
        decimal? totalDays = HasValidationError(nameof(RequestCreateViewModel.StartTime))
            || HasValidationError(nameof(RequestCreateViewModel.EndTime))
            || HasValidationError(nameof(RequestCreateViewModel.StartClock))
            || HasValidationError(nameof(RequestCreateViewModel.EndClock))
            ? null
            : await TryCalculateTotalDaysAsync(model.StartTime, model.EndTime, isHalfDayStart, isHalfDayEnd);

        // Check leave balance and auto-convert to unpaid if insufficient
        bool autoConvertedToUnpaid = false;
        if (model.RequestType == RequestType.Leave && model.LeaveReason.HasValue && totalDays.HasValue)
        {
            var shouldDeductLeave = LeaveReasonHelper.GetDeductsLeave(model.LeaveReason.Value);
            if (shouldDeductLeave)
            {
                // CRITICAL: Use current date to check leave balance, not request date
                // This ensures we validate against current available leave, not historical leave
                var summary = await _leaveService.GetBalanceSummaryAsync(employeeId, today);
                if (summary.TotalRemaining < totalDays.Value)
                {
                    // Auto-convert to unpaid instead of blocking
                    model.CountsAsWork = false;
                    autoConvertedToUnpaid = true;
                    TempData["Warning"] = $"Không đủ số dư phép (còn {summary.TotalRemaining} ngày, cần {totalDays.Value} ngày). Đơn được chuyển sang không tính công.";
                }
            }
        }

        if (model.RequestType == RequestType.Leave && model.LeaveReason.HasValue && totalDays.HasValue && !autoConvertedToUnpaid)
        {
            model.CountsAsWork = LeaveReasonHelper.GetCountsAsWork(model.LeaveReason.Value);
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
        model.IsHalfDayStart = isHalfDayStart;
        model.IsHalfDayEnd = isHalfDayEnd;

        var request = new Request
        {
            EmployeeId = employeeId,
            RequestType = model.RequestType,
            CheckInOutType = model.CheckInOutType,
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
            TotalDays = totalDays!.Value,
            CountsAsWork = model.CountsAsWork // Use the potentially auto-converted value
        };

        // Handle attachments with error handling
        if (model.Attachments?.Count > 0)
        {
            try
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
            catch (Exception ex)
            {
                TempData["Warning"] = $"Đơn đã được tạo nhưng không thể upload file đính kèm: {ex.Message}";
            }
        }

        await _requestService.CreateAsync(request, model.Approver1Id!.Value, model.Approver2Id!.Value);

        if (!autoConvertedToUnpaid && string.IsNullOrEmpty(TempData["Warning"]?.ToString()))
        {
            TempData["Success"] = "Đã gửi đơn thành công!";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await _requestService.GetWithDetailsAsync(id);
        if (request == null) return NotFound();

        var currentEmployeeId = GetCurrentEmployeeId();
        if (!CanAccessRequest(request, currentEmployeeId))
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

        var requestList = requests.ToList();

        // Batch fetch leave balances for all employees in the requests
        var employeeIds = requestList.Select(r => r.EmployeeId).Distinct().ToList();
        var leaveBalances = await _leaveService.GetBalanceSummariesAsync(employeeIds);

        var model = new PendingApprovalsViewModel
        {
            Requests = requestList,
            FilterStatus = status,
            FilterType = type,
            FilterDateFrom = from,
            FilterDateTo = to,
            CurrentEmployeeId = employeeId,
            EmployeeLeaveBalances = leaveBalances
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
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RevertApproval(int requestId, string? comment = null)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            await _requestService.RevertApprovalAsync(requestId, employeeId, comment);
            TempData["Success"] = "Đã hoàn duyệt đơn thành công! Số phép đã được hoàn trả.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi hoàn duyệt: {ex.Message}";
        }

        return RedirectToAction(nameof(Approve));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkApprove(string requestIds, string comment)
    {
        if (!IsPrivileged()) return Forbid();
        if (string.IsNullOrEmpty(requestIds))
        {
            TempData["Error"] = "Không có đơn nào được chọn.";
            return RedirectToAction(nameof(Approve));
        }

        var employeeId = GetCurrentEmployeeId();
        var ids = requestIds.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();

        int successCount = 0;
        int errorCount = 0;

        foreach (var id in ids)
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("Manager"))
                    await _requestService.ForceApproveAsync(id, employeeId, comment);
                else
                    await _requestService.ApproveAsync(id, employeeId, comment);
                successCount++;
            }
            catch
            {
                errorCount++;
            }
        }

        if (successCount > 0)
            TempData["Success"] = $"Đã duyệt thành công {successCount} đơn!";
        if (errorCount > 0)
            TempData["Warning"] = $"Có {errorCount} đơn không thể duyệt.";

        return RedirectToAction(nameof(Approve));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkReject(string requestIds, string comment)
    {
        if (!IsPrivileged()) return Forbid();
        if (string.IsNullOrEmpty(requestIds))
        {
            TempData["Error"] = "Không có đơn nào được chọn.";
            return RedirectToAction(nameof(Approve));
        }
        if (string.IsNullOrEmpty(comment))
        {
            TempData["Error"] = "Vui lòng nhập lý do từ chối.";
            return RedirectToAction(nameof(Approve));
        }

        var employeeId = GetCurrentEmployeeId();
        var ids = requestIds.Split(',').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToList();

        int successCount = 0;
        int errorCount = 0;

        foreach (var id in ids)
        {
            try
            {
                if (User.IsInRole("Admin") || User.IsInRole("Manager"))
                    await _requestService.ForceRejectAsync(id, employeeId, comment);
                else
                    await _requestService.RejectAsync(id, employeeId, comment);
                successCount++;
            }
            catch
            {
                errorCount++;
            }
        }

        if (successCount > 0)
            TempData["Success"] = $"Đã từ chối thành công {successCount} đơn!";
        if (errorCount > 0)
            TempData["Warning"] = $"Có {errorCount} đơn không thể từ chối.";

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

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && request.EmployeeId != employeeId)
        {
            TempData["Error"] = "Bạn không có quyền chỉnh sửa đơn này.";
            return RedirectToAction(nameof(Index));
        }

        // Check status - only pending requests can be edited (for everyone)
        if (request.Status != RequestStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể chỉnh sửa đơn đang chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        var model = new RequestCreateViewModel
        {
            RequestId = request.RequestId,
            RequestType = request.RequestType,
            CheckInOutType = request.CheckInOutType,
            Title = request.Title,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            StartDate = request.StartTime.Date,
            EndDate = request.EndTime.Date,
            StartClock = request.StartTime.TimeOfDay > TimeSpan.Zero ? request.StartTime.ToString("HH:mm") : null,
            EndClock = request.EndTime.TimeOfDay > TimeSpan.Zero ? request.EndTime.ToString("HH:mm") : null,
            IsHalfDayStart = request.IsHalfDayStart,
            IsHalfDayEnd = request.IsHalfDayEnd,
            HalfDayStartOption = request.IsHalfDayStart ? (request.IsHalfDayStartMorning ? 1 : 2) : 0,
            HalfDayEndOption = request.IsHalfDayEnd ? (request.IsHalfDayEndMorning ? 1 : 2) : 0,
            LeaveReason = request.LeaveReason,
            Reason = request.Reason,
            Description = request.Description,
            TotalDays = request.TotalDays
        };

        await PopulateCreateViewModel(model, request.EmployeeId); // Use request owner's ID for proper context
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RequestCreateViewModel model)
    {
        var employeeId = GetCurrentEmployeeId();
        var request = await _requestService.GetWithDetailsAsync(id);
        if (request == null) return NotFound();
        NormalizeRequiredTextFields(model);
        ValidateRequiredRequestFields(model);
        ApplyRequestDateInputs(model);
        NormalizeSingleDayRequest(model);
        ValidateTimeBasedRequest(model);

        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && request.EmployeeId != employeeId)
        {
            TempData["Error"] = "Bạn không có quyền chỉnh sửa đơn này.";
            return RedirectToAction(nameof(Index));
        }

        // Check status - only pending requests can be edited (for everyone)
        if (request.Status != RequestStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể chỉnh sửa đơn đang chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // All employees now need to select Approver1 manually
        if (!HasValidationError(nameof(RequestCreateViewModel.Approver1Id))
            && (!model.Approver1Id.HasValue || model.Approver1Id == 0))
        {
            ModelState.AddModelError("Approver1Id", "Vui lòng chọn người duyệt 1.");
        }

        // Validate request date: Cannot create request for past dates older than 5 working days
        var today = DateTimeHelper.VietnamToday;
        var requestStartDate = model.StartTime.Date;

        if (!HasValidationError(nameof(RequestCreateViewModel.StartTime)) && requestStartDate < today)
        {
            var workingDaysBetween = DateTimeHelper.CountWorkingDaysBetween(requestStartDate, today);
            // workingDaysBetween includes both start and end dates, so we need to subtract 1
            var daysAgo = workingDaysBetween - 1;

            if (daysAgo > 5)
            {
                ModelState.AddModelError("StartTime",
                    $"Không thể sửa đơn cho ngày quá xa trong quá khứ. " +
                    $"Bạn chỉ có thể tạo/sửa đơn cho tối đa 5 ngày làm việc trước (không tính T7, CN). " +
                    $"Ngày bạn chọn ({requestStartDate:dd/MM/yyyy}) cách đây {daysAgo} ngày làm việc.");
            }
        }

        var isHalfDayStart = model.HalfDayStartOption > 0;
        var isHalfDayEnd = model.HalfDayEndOption > 0;
        decimal? totalDays = HasValidationError(nameof(RequestCreateViewModel.StartTime))
            || HasValidationError(nameof(RequestCreateViewModel.EndTime))
            || HasValidationError(nameof(RequestCreateViewModel.StartClock))
            || HasValidationError(nameof(RequestCreateViewModel.EndClock))
            ? null
            : await TryCalculateTotalDaysAsync(model.StartTime, model.EndTime, isHalfDayStart, isHalfDayEnd);

        if (!ModelState.IsValid)
        {
            model.RequestId = id;
            await PopulateCreateViewModel(model, request.EmployeeId);
            return View(model);
        }

        if (model.LeaveReason.HasValue)
            model.Reason = LeaveReasonHelper.GetDisplayName(model.LeaveReason.Value);

        model.IsHalfDayStart = isHalfDayStart;
        model.IsHalfDayEnd = isHalfDayEnd;

        request.RequestType = model.RequestType;
        request.CheckInOutType = model.CheckInOutType;
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
        request.TotalDays = totalDays!.Value;

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
                countsAsWork = r.CountsAsWork,
                deductsLeave = r.DeductsLeave,
                statusText = LeaveReasonHelper.GetStatusText(r.Reason)
            });
        return Json(reasons);
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaveBalance(DateTime startDate, DateTime endDate, bool isHalfDayStart = false, bool isHalfDayEnd = false)
    {
        try
        {
            var employeeId = GetCurrentEmployeeId();
            var totalDays = await _requestService.CalculateTotalDaysAsync(startDate, endDate, isHalfDayStart, isHalfDayEnd);

            // CRITICAL: Always use current date to calculate available leave balance, not the request date
            // This ensures we calculate based on current entitlements, not past entitlements
            var currentDate = DateTimeHelper.VietnamToday;
            var summary = await _leaveService.GetBalanceSummaryAsync(employeeId, currentDate);
            var availableLeave = summary.TotalRemaining;

            return Json(new
            {
                availableLeave = availableLeave,
                totalDays = totalDays,
                hasSufficientBalance = availableLeave >= totalDays,
                message = totalDays <= 0m
                    ? "Khoảng chọn chưa phát sinh ngày làm việc, đơn vẫn có thể được tạo trước."
                    : availableLeave >= totalDays
                        ? $"Đủ số dư phép ({availableLeave} ngày)"
                        : $"Không đủ số dư phép (còn {availableLeave} ngày, cần {totalDays} ngày)"
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                availableLeave = 0m,
                totalDays = 0m,
                hasSufficientBalance = false,
                message = ex.Message
            });
        }
    }

    private async Task PopulateCreateViewModel(RequestCreateViewModel model, int employeeId)
    {
        // Everyone needs manual selection now, but source list differs by position level
        model.NeedsManualApprover1Selection = true;

        // Check if employee has low-level position to determine Approver1 source
        var departmentManagers = await _requestService.GetDepartmentManagersAsync(employeeId);

        if (departmentManagers.Any())
        {
            // Low-level position: Show department managers as Approver1 options
            model.AvailableApprover1s = departmentManagers
                .Select(m => new SelectListItem(m.FullName, m.EmployeeId.ToString()))
                .ToList();
        }
        else
        {
            // High-level position OR no managers in department: Use Approver2 list for Approver1
            var availableApprover2s = await _requestService.GetAvailableApprover2ListAsync();
            model.AvailableApprover1s = availableApprover2s
                .Where(a => a.EmployeeId != employeeId)
                .Select(a => new SelectListItem(a.FullName, a.EmployeeId.ToString()))
                .ToList();
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

    private bool CanAccessRequest(Request request, int employeeId)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        if (request.EmployeeId == employeeId)
        {
            return true;
        }

        return request.Approvals.Any(a => a.ApproverId == employeeId);
    }

    private static void NormalizeSingleDayRequest(RequestCreateViewModel model)
    {
        if (model.RequestType is RequestType.Absence or RequestType.CheckInOut or RequestType.WorkFromHome)
        {
            if (model.RequestType == RequestType.Absence)
            {
                model.EndTime = model.StartTime.Date.Add(model.EndTime.TimeOfDay);
                return;
            }

            model.EndTime = model.StartTime;
        }
    }

    private static void ApplyRequestDateInputs(RequestCreateViewModel model)
    {
        model.StartTime = model.StartDate.Date;
        model.EndTime = model.EndDate.Date;

        if (model.RequestType == RequestType.Absence)
        {
            model.EndTime = model.StartDate.Date;

            if (TimeSpan.TryParse(model.StartClock, out var absenceStart))
            {
                model.StartTime = model.StartDate.Date.Add(absenceStart);
            }

            if (TimeSpan.TryParse(model.EndClock, out var absenceEnd))
            {
                model.EndTime = model.StartDate.Date.Add(absenceEnd);
            }

            return;
        }

        if (model.RequestType == RequestType.CheckInOut)
        {
            model.EndTime = model.StartDate.Date;
            var selectedClock = model.CheckInOutType == CheckInOutType.MissedCheckOut
                ? model.EndClock
                : model.StartClock;

            if (TimeSpan.TryParse(selectedClock, out var checkInOutClock))
            {
                model.StartTime = model.StartDate.Date.Add(checkInOutClock);
                model.EndTime = model.StartTime;
            }

            return;
        }

        if (model.RequestType == RequestType.WorkFromHome)
        {
            model.EndTime = model.StartDate.Date;
        }
    }

    private void ValidateTimeBasedRequest(RequestCreateViewModel model)
    {
        if (model.RequestType == RequestType.CheckInOut)
        {
            if (!model.CheckInOutType.HasValue)
            {
                ModelState.AddModelError(nameof(RequestCreateViewModel.CheckInOutType), "Vui lòng chọn loại checkin/out.");
                return;
            }

            if (model.CheckInOutType == CheckInOutType.MissedCheckOut)
            {
                if (string.IsNullOrWhiteSpace(model.EndClock))
                {
                    ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Vui lòng chọn giờ check out.");
                    return;
                }

                if (!TimeSpan.TryParse(model.EndClock, out _))
                {
                    ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Giờ check out không hợp lệ.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(model.StartClock))
                {
                    ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Vui lòng chọn giờ check in.");
                    return;
                }

                if (!TimeSpan.TryParse(model.StartClock, out _))
                {
                    ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Giờ check in không hợp lệ.");
                }
            }

            return;
        }

        if (model.RequestType != RequestType.Absence)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(model.StartClock))
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Vui lòng chọn giờ bắt đầu.");
        }

        if (string.IsNullOrWhiteSpace(model.EndClock))
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Vui lòng chọn giờ kết thúc.");
        }

        if (!TimeSpan.TryParse(model.StartClock, out var startClock))
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.StartClock), "Giờ bắt đầu không hợp lệ.");
        }

        if (!TimeSpan.TryParse(model.EndClock, out var endClock))
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Giờ kết thúc không hợp lệ.");
        }

        if (TimeSpan.TryParse(model.StartClock, out startClock)
            && TimeSpan.TryParse(model.EndClock, out endClock)
            && endClock <= startClock)
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.EndClock), "Giờ kết thúc phải sau giờ bắt đầu.");
        }
    }

    private async Task<decimal?> TryCalculateTotalDaysAsync(DateTime startTime, DateTime endTime, bool isHalfDayStart, bool isHalfDayEnd)
    {
        try
        {
            return await _requestService.CalculateTotalDaysAsync(startTime, endTime, isHalfDayStart, isHalfDayEnd);
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.StartTime), ex.Message);
            return null;
        }
    }

    private void ValidateRequiredRequestFields(RequestCreateViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.Title), "Vui lòng nhập tiêu đề.");
        }

        if (!model.LeaveReason.HasValue)
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.LeaveReason), "Vui lòng chọn lý do.");
        }

        if (!model.Approver1Id.HasValue || model.Approver1Id.Value <= 0)
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.Approver1Id), "Vui lòng chọn người duyệt 1.");
        }

        if (!model.Approver2Id.HasValue || model.Approver2Id.Value <= 0)
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.Approver2Id), "Vui lòng chọn người duyệt 2.");
        }

        if (model.StartDate == default)
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.StartTime), "Vui lòng chọn thời gian bắt đầu.");
        }

        if (model.RequestType == RequestType.Leave && model.EndDate == default)
        {
            ModelState.AddModelError(nameof(RequestCreateViewModel.EndTime), "Vui lòng chọn thời gian kết thúc.");
        }
    }

    private static void NormalizeRequiredTextFields(RequestCreateViewModel model)
    {
        model.Title = model.Title?.Trim() ?? string.Empty;
    }

    private bool CanCalculateTotalDays(RequestCreateViewModel model)
        => !HasValidationError(nameof(RequestCreateViewModel.StartTime))
            && !HasValidationError(nameof(RequestCreateViewModel.EndTime))
            && !HasValidationError(nameof(RequestCreateViewModel.StartClock))
            && !HasValidationError(nameof(RequestCreateViewModel.EndClock))
            && model.StartDate != default
            && (model.RequestType != RequestType.Leave || model.EndDate != default);

    private bool HasValidationError(string key)
        => ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0;

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
