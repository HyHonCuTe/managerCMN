
using System.Security.Claims;
using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using managerCMN.Authorization;
using managerCMN.Data;
using managerCMN.Filters;
using managerCMN.Repositories.Implementations;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Implementations;
using managerCMN.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<SystemLogActionFilter>();

// ── Authentication (Google OAuth + Cookie) ──


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.SaveTokens = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
    options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    options.ClaimActions.MapJsonKey("picture", "picture");
});

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
});

// ── MVC ──
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<SystemLogActionFilter>();
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

// ── Auto-migrate database ──
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//     db.Database.Migrate();
//     await DataSeeder.SeedAsync(db);
// }

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
