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
        var canViewAllNotifications = CanViewAllNotifications();

        if (canViewAllNotifications)
        {
            notifications = await _notificationService.GetAllAsync();
            ViewBag.CanViewAllNotifications = true;
        }
        else
        {
            var userId = GetCurrentUserId();
            notifications = await _notificationService.GetByUserAsync(userId);
            ViewBag.CanViewAllNotifications = false;
        }

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var marked = await _notificationService.TryMarkAsReadAsync(
            id,
            GetCurrentUserId(),
            CanViewAllNotifications());

        if (!marked)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Open(int id)
    {
        var targetUrl = await _notificationService.TryOpenAsync(
            id,
            GetCurrentUserId(),
            CanViewAllNotifications());

        if (targetUrl == null)
            return RedirectToAction(nameof(Index));

        if (!Url.IsLocalUrl(targetUrl))
            return RedirectToAction(nameof(Index));

        return Redirect(targetUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        if (CanViewAllNotifications())
        {
            await _notificationService.MarkAllAsReadAsync();
        }
        else
        {
            await _notificationService.MarkAllAsReadAsync(GetCurrentUserId());
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var count = CanViewAllNotifications()
            ? await _notificationService.GetAllUnreadCountAsync()
            : await _notificationService.GetUnreadCountAsync(GetCurrentUserId());

        return Json(new { count });
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private bool CanViewAllNotifications()
        => User.IsInRole("Admin");
}
