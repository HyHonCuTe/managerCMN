using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;

    public NotificationController(INotificationService notificationService, ApplicationDbContext db)
    {
        _notificationService = notificationService;
        _db = db;
    }

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

        // Load scheduled announcements for admin users
        if (canViewAllNotifications)
        {
            var announcements = await _db.ScheduledAnnouncements
                .Include(a => a.FilterDepartment)
                .OrderByDescending(a => a.ScheduledAt)
                .Take(50)
                .ToListAsync();
            ViewBag.Announcements = announcements;

            // Load departments and active employees for create form
            var departments = await _db.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();
            ViewBag.Departments = departments;

            var activeEmployees = await _db.Employees
                .Include(e => e.Department)
                .Where(e => e.Status == Models.Enums.EmployeeStatus.Active)
                .OrderBy(e => e.Department != null ? e.Department.DepartmentName : "")
                .ThenBy(e => e.FullName)
                .ToListAsync();
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.AnnouncementAudiences = BuildAnnouncementAudiences(announcements, activeEmployees);
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

    private static List<ScheduledAnnouncementAudienceViewModel> BuildAnnouncementAudiences(
        IReadOnlyCollection<ScheduledAnnouncement> announcements,
        IReadOnlyCollection<Employee> activeEmployees)
    {
        return announcements
            .Select(announcement =>
            {
                var recipients = ResolveAnnouncementRecipients(announcement, activeEmployees);
                return new ScheduledAnnouncementAudienceViewModel
                {
                    AnnouncementId = announcement.AnnouncementId,
                    RecipientCount = recipients.Count,
                    TargetSummary = BuildTargetSummary(announcement, recipients.Count),
                    RecipientNames = recipients.Select(employee => employee.FullName).ToList()
                };
            })
            .ToList();
    }

    private static List<Employee> ResolveAnnouncementRecipients(
        ScheduledAnnouncement announcement,
        IReadOnlyCollection<Employee> activeEmployees)
    {
        if (!string.IsNullOrWhiteSpace(announcement.FilterEmployeeIds))
        {
            List<int>? specificIds;
            try
            {
                specificIds = JsonSerializer.Deserialize<List<int>>(announcement.FilterEmployeeIds);
            }
            catch
            {
                specificIds = null;
            }

            if (specificIds == null || specificIds.Count == 0)
            {
                return [];
            }

            return activeEmployees
                .Where(employee => specificIds.Contains(employee.EmployeeId))
                .OrderBy(employee => employee.FullName)
                .ToList();
        }

        IEnumerable<Employee> query = activeEmployees;

        if (announcement.FilterDepartmentId.HasValue)
        {
            query = query.Where(employee => employee.DepartmentId == announcement.FilterDepartmentId.Value);
        }

        if (announcement.FilterGender.HasValue)
        {
            query = query.Where(employee => (int)employee.Gender == announcement.FilterGender.Value);
        }

        return query
            .OrderBy(employee => employee.Department != null ? employee.Department.DepartmentName : "")
            .ThenBy(employee => employee.FullName)
            .ToList();
    }

    private static string BuildTargetSummary(ScheduledAnnouncement announcement, int recipientCount)
    {
        if (recipientCount == 0)
        {
            return "Không có người nhận phù hợp";
        }

        if (!string.IsNullOrWhiteSpace(announcement.FilterEmployeeIds))
        {
            return $"Nhân viên cụ thể ({recipientCount})";
        }

        var targetParts = new List<string>();
        if (announcement.FilterDepartment != null)
        {
            targetParts.Add(announcement.FilterDepartment.DepartmentName);
        }

        if (announcement.FilterGender.HasValue)
        {
            targetParts.Add(announcement.FilterGender.Value switch
            {
                0 => "Nam",
                1 => "Nữ",
                _ => "Khác"
            });
        }

        if (targetParts.Count > 0)
        {
            return $"Lọc: {string.Join(" · ", targetParts)} ({recipientCount})";
        }

        return $"Tất cả nhân viên hoạt động ({recipientCount})";
    }
}
