using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
        => _dashboardService = dashboardService;

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            TotalEmployees = await _dashboardService.GetTotalEmployeesAsync(),
            PendingRequests = await _dashboardService.GetPendingRequestsCountAsync(),
            ActiveTickets = await _dashboardService.GetActiveTicketsCountAsync(),
            TotalAssets = await _dashboardService.GetTotalAssetsAsync(),
            ExpiringContracts = await _dashboardService.GetExpiringContractsAsync()
        };

        return View(model);
    }
}
