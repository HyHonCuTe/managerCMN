using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class SystemLogController : Controller
{
    private readonly ISystemLogService _logService;

    public SystemLogController(ISystemLogService logService) => _logService = logService;

    public async Task<IActionResult> Index(string? module, DateTime? from, DateTime? to)
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

        ViewBag.Module = module;
        ViewBag.From = from;
        ViewBag.To = to;
        return View(logs);
    }
}
