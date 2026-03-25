using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class SystemLogController : Controller
{
    private readonly ISystemLogService _logService;

    public SystemLogController(ISystemLogService logService) => _logService = logService;

    public async Task<IActionResult> Index(string? module, DateTime? from, DateTime? to, int page = 1, int pageSize = 50)
    {
        IEnumerable<Models.Entities.SystemLog> logs;

        if (!string.IsNullOrEmpty(module))
        {
            logs = await _logService.GetByModuleAsync(module);
        }
        else if (from.HasValue && to.HasValue)
        {
            logs = await _logService.GetByDateRangeAsync(from.Value, to.Value);
        }
        else
        {
            logs = await _logService.GetAllAsync();
        }

        // Apply pagination
        var totalCount = logs.Count();
        var pagedLogs = logs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.Module = module;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return View(pagedLogs);
    }
}
