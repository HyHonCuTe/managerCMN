
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Authorization;
using managerCMN.Data;
using managerCMN.Filters;
using managerCMN.Repositories.Implementations;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Implementations;
using managerCMN.Services.Interfaces;

LoadDotEnvIfPresent();

var builder = WebApplication.CreateBuilder(args);
var requireSecureCookies = !builder.Environment.IsDevelopment();
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

// ── Database ──
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repository & UoW ──
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Services ──
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<IMeetingRoomService, MeetingRoomService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IHolidayService, HolidayService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IPostHistoryService, PostHistoryService>();
builder.Services.AddScoped<IProjectAccessService, ProjectAccessService>();
builder.Services.AddScoped<IProjectProgressService, ProjectProgressService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectTaskService, ProjectTaskService>();
builder.Services.AddScoped<SystemLogActionFilter>();

// ── Authentication (Google OAuth + Cookie) ──


var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = requireSecureCookies
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});

if (HasConfiguredSecret(googleClientId) && HasConfiguredSecret(googleClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/signin-google";
        // The app does not read Google access/refresh tokens later, so keeping them only bloats the callback cookie.
        options.SaveTokens = false;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
        options.ClaimActions.MapJsonKey("picture", "picture");
    });
}

// ── Authorization ──
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    // Role-based policies (backward compatibility)
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MasterAdminOnly", policy => policy
        .RequireRole("Admin")
        .RequireClaim("EmployeeCode", "A00000"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());

    // Permission-based policies
    options.AddPolicy("ManagePermissions", policy =>
        policy.Requirements.Add(new PermissionRequirement("Settings.ManagePermissions")));
    options.AddPolicy("ViewEmployees", policy =>
        policy.Requirements.Add(new PermissionRequirement("Employee.View")));
    options.AddPolicy("ViewProjects", policy =>
        policy.Requirements.Add(new PermissionRequirement("Project.View")));
    options.AddPolicy("CreateProject", policy =>
        policy.Requirements.Add(new PermissionRequirement("Project.Create")));
    options.AddPolicy("ManageProjectTask", policy =>
        policy.Requirements.Add(new PermissionRequirement("ProjectTask.Manage")));
});

// ── MVC ──
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<SystemLogActionFilter>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = requireSecureCookies
        ? "__Host-managerCMN.Antiforgery"
        : "managerCMN.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = requireSecureCookies
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});

// ── Localization ──
var supportedCultures = new[] { new CultureInfo("vi-VN") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("vi-VN");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// ── HttpContextAccessor for Service logging ──
builder.Services.AddHttpContextAccessor();

var app = builder.Build();


// ── Configure Forwarded Headers for Nginx proxy ──
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ── Middleware pipeline ──
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Handle HTTP status codes (404, 500, 502, 503, 504, etc.)
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "base-uri 'self'; " +
            "object-src 'none'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self' https://accounts.google.com; " +
            "img-src 'self' data: https:; " +
            "font-src 'self' data: https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdn.datatables.net; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdn.datatables.net https://code.jquery.com; " +
            "connect-src 'self' https://cdn.datatables.net;";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return Task.CompletedTask;
    });

    await next();
});

app.UseStaticFiles();

var localizationOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

static bool HasConfiguredSecret(string? value)
    => !string.IsNullOrWhiteSpace(value)
        && !value.Contains("SET_IN_ENV", StringComparison.OrdinalIgnoreCase)
        && !value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);

static void LoadDotEnvIfPresent()
{
    foreach (var candidatePath in EnumerateDotEnvCandidates())
    {
        if (!File.Exists(candidatePath))
            continue;

        foreach (var rawLine in File.ReadLines(candidatePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var value = line[(separatorIndex + 1)..].Trim();
            if (value.Length >= 2
                && ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        break;
    }
}

static IEnumerable<string> EnumerateDotEnvCandidates()
{
    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var basePath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        if (string.IsNullOrWhiteSpace(basePath))
            continue;

        var directory = new DirectoryInfo(basePath);
        while (directory != null && visited.Add(directory.FullName))
        {
            yield return Path.Combine(directory.FullName, ".env");
            directory = directory.Parent;
        }
    }
}
