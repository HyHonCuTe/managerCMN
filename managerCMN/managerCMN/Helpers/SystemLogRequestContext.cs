namespace managerCMN.Helpers;

public static class SystemLogRequestContext
{
    private const string HasWrittenLogKey = "__SystemLogWritten";

    public static bool HasWrittenLog(HttpContext? httpContext)
        => httpContext?.Items.TryGetValue(HasWrittenLogKey, out var value) == true &&
           value is true;

    public static void MarkWritten(HttpContext? httpContext)
    {
        if (httpContext == null)
            return;

        httpContext.Items[HasWrittenLogKey] = true;
    }
}
