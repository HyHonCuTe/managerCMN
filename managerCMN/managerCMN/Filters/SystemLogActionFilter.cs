using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using managerCMN.Attributes;
using managerCMN.Helpers;
using managerCMN.Services.Interfaces;

namespace managerCMN.Filters;

public class SystemLogActionFilter : IAsyncActionFilter
{
    private readonly ISystemLogService _logService;
    private static readonly HashSet<string> IgnoredRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Notification/UnreadCount"
    };

    public SystemLogActionFilter(ISystemLogService logService)
    {
        _logService = logService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await next();

        if (!ShouldLog(context, result))
            return;

        var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown";
        var actionName = context.RouteData.Values["action"]?.ToString() ?? "Unknown";
        var module = controller;
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
        var statusCode = result.HttpContext.Response.StatusCode;
        var httpMethod = context.HttpContext.Request.Method;
        var route = $"{controller}/{actionName}";
        var arguments = BuildSafeValue(context.ActionArguments, "Arguments", 0);
        var query = BuildSafeValue(
            context.HttpContext.Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString()),
            "Query",
            0);
        var action = route;

        var dataBefore = new
        {
            Method = httpMethod,
            Route = route,
            Query = query,
            Arguments = arguments
        };

        var dataAfter = new
        {
            StatusCode = statusCode,
            ResultType = result.Result?.GetType().Name,
            Error = result.Exception is { } ex && !result.ExceptionHandled
                ? new { ex.Message, ExceptionType = ex.GetType().Name }
                : null
        };

        _ = int.TryParse(userId, out var uid);
        await _logService.LogAsync(uid > 0 ? uid : null, action, module, dataBefore, dataAfter, ip);
    }

    private static bool ShouldLog(ActionExecutingContext context, ActionExecutedContext result)
    {
        if (SystemLogRequestContext.HasWrittenLog(context.HttpContext))
            return false;

        var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;
        var route = $"{controller}/{action}";
        if (IgnoredRoutes.Contains(route))
            return false;

        var isApiKeyProtected = context.ActionDescriptor is ControllerActionDescriptor cad &&
                                (cad.MethodInfo.IsDefined(typeof(ApiKeyAuthenticationAttribute), inherit: true) ||
                                 cad.ControllerTypeInfo.IsDefined(typeof(ApiKeyAuthenticationAttribute), inherit: true));

        if (context.HttpContext.User.Identity?.IsAuthenticated != true && !isApiKeyProtected)
            return false;

        if (HttpMethods.IsGet(context.HttpContext.Request.Method) ||
            HttpMethods.IsHead(context.HttpContext.Request.Method) ||
            HttpMethods.IsOptions(context.HttpContext.Request.Method) ||
            HttpMethods.IsTrace(context.HttpContext.Request.Method))
            return false;

        if (context.ActionDescriptor is not ControllerActionDescriptor)
            return false;

        return true;
    }

    private static object? BuildSafeValue(object? value, string? propertyName, int depth)
    {
        if (value == null)
            return null;

        if (depth > 2)
            return value.ToString();

        var type = value.GetType();

        if (IsSensitiveProperty(propertyName))
            return "***";

        if (type.IsEnum ||
            type.IsPrimitive ||
            value is decimal ||
            value is Guid ||
            value is DateTime ||
            value is DateTimeOffset ||
            value is DateOnly ||
            value is TimeOnly)
            return value;

        if (value is string stringValue)
            return stringValue.Length <= 200 ? stringValue : $"{stringValue[..200]}...";

        if (value is IFormFile file)
        {
            return new
            {
                file.FileName,
                file.Length,
                file.ContentType
            };
        }

        if (value is IEnumerable<IFormFile> files)
        {
            return files.Select(fileItem => new
            {
                fileItem.FileName,
                fileItem.Length,
                fileItem.ContentType
            }).ToList();
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var entries = new Dictionary<string, object?>();
            foreach (var key in dictionary.Keys)
            {
                var dictionaryKey = key?.ToString() ?? string.Empty;
                var dictionaryValue = key != null ? dictionary[key] : null;
                entries[dictionaryKey] = BuildSafeValue(dictionaryValue, dictionaryKey, depth + 1);
            }

            return entries;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            var items = new List<object?>();
            foreach (var item in enumerable)
            {
                items.Add(BuildSafeValue(item, propertyName, depth + 1));
                if (items.Count >= 20)
                    break;
            }

            return items;
        }

        var properties = type
            .GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Take(20);

        var safeObject = new Dictionary<string, object?>();
        foreach (var property in properties)
        {
            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch
            {
                propertyValue = "[Unreadable]";
            }

            safeObject[property.Name] = BuildSafeValue(propertyValue, property.Name, depth + 1);
        }

        return safeObject;
    }

    private static bool IsSensitiveProperty(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return false;

        var normalized = propertyName.ToLowerInvariant();
        return normalized.Contains("password") ||
               normalized.Contains("secret") ||
               normalized.Contains("token") ||
               normalized.Contains("apikey") ||
               normalized.Contains("api_key") ||
               normalized.Contains("taxcode") ||
               normalized.Contains("bankaccount") ||
               normalized.Contains("insurancecode") ||
               normalized.Contains("idcard") ||
               normalized.Contains("googleid");
    }
}
