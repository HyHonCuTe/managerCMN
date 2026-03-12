using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using managerCMN.Services.Interfaces;

namespace managerCMN.Filters;

public class SystemLogActionFilter : IAsyncActionFilter
{
    private readonly ISystemLogService _logService;

    public SystemLogActionFilter(ISystemLogService logService)
    {
        _logService = logService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();

        if (context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var action = $"{context.RouteData.Values["controller"]}/{context.RouteData.Values["action"]}";
            var module = context.RouteData.Values["controller"]?.ToString();
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();

            _ = int.TryParse(userId, out var uid);
            await _logService.LogAsync(uid > 0 ? uid : null, action, module, null, null, ip);
        }
    }
}
