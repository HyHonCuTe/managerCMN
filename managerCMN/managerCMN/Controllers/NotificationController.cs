using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.Entities;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
        => _notificationService = notificationService;

    public async Task<IActionResult> Index()
    {
        IEnumerable<Notification> notifications;

        if (IsPrivileged())
        {
            notifications = await _notificationService.GetAllAsync();
            ViewBag.IsPrivileged = true;
        }
        else
        {
            var userId = GetCurrentUserId();
            notifications = await _notificationService.GetByUserAsync(userId);
            ViewBag.IsPrivileged = false;
        }

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        if (IsPrivileged())
        {
            // Admin/Manager: Mark ALL notifications as read (system-wide)
            await _notificationService.MarkAllAsReadAsync();
        }
        else
        {
            // Regular user: Mark only their notifications as read
            await _notificationService.MarkAllAsReadAsync(GetCurrentUserId());
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync(GetCurrentUserId());
        return Json(new { count });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private bool IsPrivileged()
        => User.IsInRole("Admin") || User.IsInRole("Manager") || User.HasClaim("IsApprover", "true");
}
