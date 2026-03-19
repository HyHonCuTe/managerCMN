using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        var model = new TicketIndexViewModel
        {
            ReceivedTickets = await _ticketService.GetReceivedTicketsAsync(employeeId),
            SentTickets = await _ticketService.GetSentTicketsAsync(employeeId),
            AllTickets = isAdmin ? await _ticketService.GetAllTicketsAsync() : Enumerable.Empty<Ticket>(),
            IsAdmin = isAdmin,
            ActiveTab = tab ?? "received"
        };

        return View(model);
    }

    #endregion

    #region Create

    public async Task<IActionResult> Create()
    {
        var employeeId = GetCurrentEmployeeId();
        var model = new TicketCreateViewModel
        {
            AvailableRecipients = (await _ticketService.GetAvailableRecipientsAsync(employeeId))
                .Select(e => new SelectListItem(e.FullName, e.EmployeeId.ToString()))
                .ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRecipients = (await _ticketService.GetAvailableRecipientsAsync(GetCurrentEmployeeId()))
                .Select(e => new SelectListItem(e.FullName, e.EmployeeId.ToString()))
                .ToList();
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
            CanReply = isCreator || isRecipient,
            CanForward = isRecipient || isAdmin,
            CanUpdateStatus = isRecipient && currentRecipient?.Status != TicketRecipientStatus.Completed,
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

        await _ticketService.ReplyAsync(ticketId, employeeId, content, attachments);
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

        await _ticketService.ForwardAsync(ticketId, employeeId, recipientIds, content ?? string.Empty, attachments);
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

        var recipient = ticket.Recipients.FirstOrDefault(r => r.EmployeeId == GetCurrentEmployeeId());
        if (recipient == null) return Forbid();

        await _ticketService.UpdateRecipientStatusAsync(recipient.TicketRecipientId, status);
        TempData["Success"] = "Đã cập nhật trạng thái!";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    #endregion

    #region DownloadAttachment

    public async Task<IActionResult> DownloadAttachment(int attachmentId)
    {
        var attachment = await _ticketService.GetAttachmentAsync(attachmentId);
        if (attachment == null) return NotFound();

        var filePath = Path.Combine(_env.WebRootPath, attachment.FilePath.TrimStart('/'));
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
        await _ticketService.ResolveAsync(id, resolution);
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
}
