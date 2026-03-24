using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace managerCMN.Attributes;

/// <summary>
/// API Key authentication attribute for securing API endpoints
/// Validates X-API-Key header against configured API key
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuthenticationAttribute : Attribute, IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKey = config["ApiSettings:AttendanceApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Result = new ObjectResult(new { error = "API Key not configured on server" })
            {
                StatusCode = 500
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "API Key required in X-API-Key header" });
            return;
        }

        if (!string.Equals(apiKey, providedApiKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid API Key" });
            return;
        }

        // API key is valid, continue processing
    }
}