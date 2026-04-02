using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "MasterAdminOnly")]
public class SystemLogController : Controller
{
    private readonly ISystemLogService _logService;
    private readonly ApplicationDbContext _db;
    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public SystemLogController(ISystemLogService logService, ApplicationDbContext db)
    {
        _logService = logService;
        _db = db;
    }

    public async Task<IActionResult> Index(string? module, string? logAction, DateTime? from, DateTime? to)
    {
        var logs = await _logService.SearchAsync(module, logAction, from, to);
        var userIds = logs
            .Where(log => log.UserId.HasValue)
            .Select(log => log.UserId!.Value)
            .Distinct()
            .ToArray();

        var userLookup = await _db.Users
            .AsNoTracking()
            .Include(user => user.Employee)
            .Where(user => userIds.Contains(user.UserId))
            .ToDictionaryAsync(user => user.UserId, BuildUserDisplayName);

        var moduleOptions = await _db.Set<Models.Entities.SystemLog>()
            .AsNoTracking()
            .Where(log => !string.IsNullOrWhiteSpace(log.Module))
            .Select(log => log.Module!)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync();

        var actionQuery = _db.Set<Models.Entities.SystemLog>()
            .AsNoTracking()
            .Where(log => !string.IsNullOrWhiteSpace(log.Action));

        if (!string.IsNullOrWhiteSpace(module))
        {
            actionQuery = actionQuery.Where(log => log.Module == module);
        }

        var actionOptions = await actionQuery
            .Select(log => log.Action)
            .Distinct()
            .OrderBy(value => value)
            .ToListAsync();

        var viewModel = new SystemLogIndexViewModel
        {
            Module = module,
            LogAction = logAction,
            From = from,
            To = to,
            TotalCount = logs.Count,
            ModuleOptions = moduleOptions,
            ActionOptions = actionOptions,
            Logs = logs.Select(log => new SystemLogListItemViewModel
            {
                Log = log,
                UserDisplayName = ResolveUserDisplayName(log.UserId, userLookup),
                DetailPreview = BuildDetailPreview(log),
                DataBeforePretty = FormatJson(log.DataBefore),
                DataAfterPretty = FormatJson(log.DataAfter),
                HasBeforeData = !string.IsNullOrWhiteSpace(log.DataBefore),
                HasAfterData = !string.IsNullOrWhiteSpace(log.DataAfter)
            }).ToList()
        };

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Xem nhat ky he thong",
            "SystemLog",
            null,
            new
            {
                ModuleFilter = module,
                ActionFilter = logAction,
                From = from,
                To = to
            },
            GetClientIP());

        return View("IndexEnhanced", viewModel);
    }

    private static string BuildUserDisplayName(Models.Entities.User user)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(user.FullName))
            parts.Add(user.FullName);

        if (!string.IsNullOrWhiteSpace(user.Email))
            parts.Add(user.Email);

        if (!string.IsNullOrWhiteSpace(user.Employee?.EmployeeCode))
            parts.Add(user.Employee.EmployeeCode);

        return parts.Count > 0 ? string.Join(" | ", parts) : $"User #{user.UserId}";
    }

    private static string ResolveUserDisplayName(int? userId, IReadOnlyDictionary<int, string> userLookup)
    {
        if (!userId.HasValue)
            return "He thong";

        return userLookup.TryGetValue(userId.Value, out var displayName)
            ? displayName
            : $"User #{userId.Value}";
    }

    private static string BuildDetailPreview(Models.Entities.SystemLog log)
    {
        var source = !string.IsNullOrWhiteSpace(log.DataAfter) ? log.DataAfter : log.DataBefore;
        if (string.IsNullOrWhiteSpace(source))
            return "Khong co du lieu chi tiet";

        var formatted = FormatJson(source)
            .Replace(Environment.NewLine, " ")
            .Trim();

        return formatted.Length <= 140 ? formatted : $"{formatted[..140]}...";
    }

    private static string FormatJson(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return "Khong co du lieu";

        try
        {
            using var document = JsonDocument.Parse(rawJson);
            return JsonSerializer.Serialize(document.RootElement, PrettyJsonOptions);
        }
        catch (JsonException)
        {
            return rawJson;
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP()
        => HttpContext.Connection.RemoteIpAddress?.ToString();
}
