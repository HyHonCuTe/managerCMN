using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class LeaveController : Controller
{
    private readonly ILeaveService _leaveService;

    public LeaveController(ILeaveService leaveService) => _leaveService = leaveService;

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Request");
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequestCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var employeeId = GetCurrentEmployeeId();
        if (employeeId == 0) return RedirectToAction("Login", "Account");

        var request = new LeaveRequest
        {
            EmployeeId = employeeId,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Reason = model.Reason,
            TotalDays = model.TotalDays > 0 ? model.TotalDays : (decimal)(model.EndDate - model.StartDate).TotalDays + 1
        };

        await _leaveService.CreateRequestAsync(request);
        return RedirectToAction("Index", "Request");
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Pending()
    {
        var requests = await _leaveService.GetPendingRequestsAsync();
        return View(requests);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var userId = GetCurrentUserId();
        await _leaveService.ApproveRequestAsync(id, userId);
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = GetCurrentUserId();
        await _leaveService.RejectRequestAsync(id, userId);
        return RedirectToAction(nameof(Pending));
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
