using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly IEmployeeService _employeeService;
    private readonly IWebHostEnvironment _env;

    public TicketController(ITicketService ticketService, IEmployeeService employeeService, IWebHostEnvironment env)
    {
        _ticketService = ticketService;
        _employeeService = employeeService;
        _env = env;
    }

    #region Index with tabs

    public async Task<IActionResult> Index(string? tab)
    {
        var employeeId = GetCurrentEmployeeId();
        var isAdmin = User.IsInRole("Admin");
        var receivedTickets = (await _ticketService.GetReceivedTicketsAsync(employeeId)).ToList();
        var sentTickets = (await _ticketService.GetSentTicketsAsync(employeeId)).ToList();
        var expiredTickets = receivedTickets
            .Concat(sentTickets)
            .Where(ticket => ticket.IsExpired())
            .GroupBy(ticket => ticket.TicketId)
            .Select(group => group.First())
            .OrderByDescending(ticket => ticket.GetDeadlineDate())
            .ThenByDescending(ticket => ticket.CreatedDate)
            .ToList();
        var availableTabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "received",
            "sent",
            "expired"
        };

        if (isAdmin)
        {
            availableTabs.Add("all");
        }

        HashSet<int> starredTicketIds = [];

        try
        {
            starredTicketIds = await _ticketService.GetStarredTicketIdsAsync(employeeId);
        }
        catch (SqlException ex) when (IsMissingTicketStarTable(ex))
        {
            starredTicketIds = [];
        }

        var model = new TicketIndexViewModel
        {
            ReceivedTickets = receivedTickets.Where(ticket => !ticket.IsExpired()).ToList(),
            SentTickets = sentTickets.Where(ticket => !ticket.IsExpired()).ToList(),
            ExpiredTickets = expiredTickets,
            AllTickets = isAdmin ? await _ticketService.GetAllTicketsAsync() : Enumerable.Empty<Ticket>(),
            StarredTicketIds = starredTicketIds,
            IsAdmin = isAdmin,
            ActiveTab = !string.IsNullOrWhiteSpace(tab) && availableTabs.Contains(tab)
                ? tab
                : "received"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStar(int ticketId)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
            return Unauthorized(new { success = false });

        try
        {
            var isStarred = await _ticketService.ToggleStarAsync(ticketId, employeeId, User.IsInRole("Admin"));
            return Json(new { success = true, isStarred });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { success = false });
        }
        catch (SqlException ex) when (IsMissingTicketStarTable(ex))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { success = false });
        }
    }

    #endregion

    #region Create

    public async Task<IActionResult> Create()
    {
        var employeeId = GetCurrentEmployeeId();
        var model = new TicketCreateViewModel
        {
            AvailableRecipients = await BuildRecipientSelectListAsync(employeeId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRecipients = await BuildRecipientSelectListAsync(GetCurrentEmployeeId());
            return View(model);
        }

        var ticket = new Ticket
        {
            Title = model.Title,
            Description = model.Description,
            Priority = model.Priority,
            Urgency = model.Urgency,
            Deadline = model.Deadline
        };

        await _ticketService.CreateWithRecipientsAsync(ticket, model.RecipientIds, model.Attachments, GetCurrentEmployeeId());
        TempData["Success"] = "Đã gửi ticket thành công!";
        return RedirectToAction(nameof(Index), new { tab = "sent" });
    }

    #endregion

    #region Details

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _ticketService.GetTicketDetailAsync(id);
        if (ticket == null) return NotFound();

        var employeeId = GetCurrentEmployeeId();
        var isCreator = ticket.CreatedBy == employeeId;
        var currentRecipient = ticket.Recipients.FirstOrDefault(r => r.EmployeeId == employeeId);
        var isRecipient = currentRecipient != null;

        // Mark as read
        if (isRecipient)
        {
            await _ticketService.MarkAsReadAsync(id, employeeId);
        }

        // Check permissions
        var isAdmin = User.IsInRole("Admin");
        if (!isCreator && !isRecipient && !isAdmin)
            return Forbid();

        // Get existing recipients to exclude from forward list
        var existingRecipientIds = ticket.Recipients.Select(r => r.EmployeeId).ToHashSet();
        existingRecipientIds.Add(ticket.CreatedBy); // Also exclude creator

        var model = new TicketDetailViewModel
        {
            Ticket = ticket,
            IsCreator = isCreator,
            IsRecipient = isRecipient,
            CurrentRecipient = currentRecipient,
            CanReply = (isCreator || isRecipient)
                       && !ticket.IsExpired()
                       && !ticket.Status.IsTerminal()
                       && (isCreator || currentRecipient?.Status != TicketRecipientStatus.Completed),
            CanForward = isRecipient || isAdmin,
            CanUpdateStatus = isRecipient
                              && currentRecipient?.Status != TicketRecipientStatus.Completed
                              && !ticket.IsExpired()
                              && !ticket.Status.IsTerminal(),
            AvailableRecipients = (await _ticketService.GetAvailableRecipientsAsync())
                .Where(e => !existingRecipientIds.Contains(e.EmployeeId))
                .Select(e => new SelectListItem(e.FullName, e.EmployeeId.ToString()))
                .ToList()
        };

        return View(model);
    }

    #endregion

    #region Reply

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int ticketId, string content, List<IFormFile>? attachments)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
        {
            TempData["Error"] = "Không tìm thấy thông tin nhân viên. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Vui lòng nhập nội dung phản hồi.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        try
        {
            await _ticketService.ReplyAsync(ticketId, employeeId, content, attachments);
            TempData["Success"] = "Đã gửi phản hồi!";
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Ticket không tồn tại hoặc đã bị xóa.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "Bạn không có quyền phản hồi ticket này.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }
        TempData["Success"] = "Đã gửi phản hồi!";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    #endregion

    #region Forward

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Forward(int ticketId, List<int> recipientIds, string? content, List<IFormFile>? attachments)
    {
        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0)
        {
            TempData["Error"] = "Không tìm thấy thông tin nhân viên. Vui lòng đăng nhập lại.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        if (recipientIds == null || !recipientIds.Any())
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một người nhận.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        try
        {
            await _ticketService.ForwardAsync(ticketId, employeeId, recipientIds, content ?? string.Empty, attachments);
            TempData["Success"] = "Đã chuyển tiếp ticket!";
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Ticket không tồn tại hoặc đã bị xóa.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "Bạn không có quyền chuyển tiếp ticket này.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }
        TempData["Success"] = "Đã chuyển tiếp ticket!";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    #endregion

    #region UpdateStatus

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int ticketId, TicketRecipientStatus status)
    {
        var ticket = await _ticketService.GetTicketDetailAsync(ticketId);
        if (ticket == null) return NotFound();

        if (ticket.IsExpired())
        {
            TempData["Error"] = "Ticket đã hết hạn nên không thể cập nhật trạng thái.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        if (ticket.Status.IsTerminal())
        {
            TempData["Error"] = "Ticket đã hoàn thành/đóng nên không thể cập nhật trạng thái nữa.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var recipient = ticket.Recipients.FirstOrDefault(r => r.EmployeeId == GetCurrentEmployeeId());
        if (recipient == null) return Forbid();

        try
        {
            await _ticketService.UpdateRecipientStatusAsync(recipient.TicketRecipientId, status);
        }
        catch (InvalidOperationException)
        {
            TempData["Error"] = "Ticket đã hết hạn nên không thể cập nhật trạng thái.";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        TempData["Success"] = "Đã cập nhật trạng thái!";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    #endregion

    #region DownloadAttachment

    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        var attachment = await _ticketService.GetAttachmentAsync(attachmentId);
        if (attachment == null) return NotFound();

        var ticketId = attachment.TicketId ?? attachment.TicketMessage?.TicketId;
        if (!ticketId.HasValue)
            return NotFound();

        var ticket = await _ticketService.GetTicketDetailAsync(ticketId.Value);
        if (ticket == null || !CanAccessTicket(ticket))
            return Forbid();

        var webRootPath = Path.GetFullPath(_env.WebRootPath);
        var relativePath = attachment.FilePath
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));

        if (!filePath.StartsWith(webRootPath, StringComparison.OrdinalIgnoreCase))
            return Forbid();
        if (!System.IO.File.Exists(filePath)) return NotFound();

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, attachment.ContentType ?? "application/octet-stream", attachment.FileName);
    }

    #endregion

    #region Legacy actions (backward compat)

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Assign(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket == null) return NotFound();

        var employees = await _employeeService.GetAllAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeId", "FullName");
        return View(ticket);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int assigneeId)
    {
        await _ticketService.AssignAsync(id, assigneeId);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, string resolution)
    {
        try
        {
            await _ticketService.ResolveAsync(id, resolution);
            TempData["Success"] = "Đã cập nhật ticket thành đã giải quyết.";
        }
        catch (KeyNotFoundException)
        {
            TempData["Error"] = "Ticket không tồn tại hoặc đã bị xóa.";
            return RedirectToAction(nameof(Index));
        }
        catch (UnauthorizedAccessException)
        {
            TempData["Error"] = "Bạn không có quyền giải quyết ticket này.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        await _ticketService.CloseAsync(id);
        return RedirectToAction(nameof(Index));
    }

    #endregion

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private static bool IsMissingTicketStarTable(SqlException ex)
        => ex.Message.Contains("TicketStars", StringComparison.OrdinalIgnoreCase);

    private bool CanAccessTicket(Ticket ticket)
    {
        var employeeId = GetCurrentEmployeeId();
        return User.IsInRole("Admin")
            || ticket.CreatedBy == employeeId
            || ticket.Recipients.Any(recipient => recipient.EmployeeId == employeeId);
    }

    private async Task<List<SelectListItem>> BuildRecipientSelectListAsync(int? excludeEmployeeId = null)
    {
        var groupCache = new Dictionary<string, SelectListGroup>(StringComparer.OrdinalIgnoreCase);

        return (await _ticketService.GetAvailableRecipientsAsync(excludeEmployeeId))
            .Select(employee =>
            {
                var departmentName = string.IsNullOrWhiteSpace(employee.Department?.DepartmentName)
                    ? "Chưa phân phòng ban"
                    : employee.Department!.DepartmentName;

                if (!groupCache.TryGetValue(departmentName, out var group))
                {
                    group = new SelectListGroup { Name = departmentName };
                    groupCache[departmentName] = group;
                }

                return new SelectListItem(employee.FullName, employee.EmployeeId.ToString())
                {
                    Group = group
                };
            })
            .OrderBy(item => item.Group?.Name)
            .ThenBy(item => item.Text)
            .ToList();
    }
}
