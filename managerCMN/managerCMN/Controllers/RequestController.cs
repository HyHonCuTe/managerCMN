using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class RequestController : Controller
{
    private readonly IRequestService _requestService;
    private readonly IWebHostEnvironment _env;

    public RequestController(IRequestService requestService, IWebHostEnvironment env)
    {
        _requestService = requestService;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var employeeId = GetCurrentEmployeeId();
        var requests = await _requestService.GetByEmployeeAsync(employeeId);
        return View(requests);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RequestCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var employeeId = GetCurrentEmployeeId();

        var request = new Request
        {
            EmployeeId = employeeId,
            RequestType = model.RequestType,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            Reason = model.Reason
        };

        // Handle attachments
        if (model.Attachments?.Count > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "requests");
            Directory.CreateDirectory(uploadsDir);

            foreach (var file in model.Attachments)
            {
                if (file.Length == 0) continue;
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                request.Attachments.Add(new RequestAttachment
                {
                    FileName = file.FileName,
                    FilePath = $"/uploads/requests/{fileName}"
                });
            }
        }

        await _requestService.CreateAsync(request);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var request = await _requestService.GetWithAttachmentsAsync(id);
        if (request == null) return NotFound();
        return View(request);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Pending()
    {
        var requests = await _requestService.GetByStatusAsync(RequestStatus.Pending);
        return View(requests);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> PendingHR()
    {
        var requests = await _requestService.GetByStatusAsync(RequestStatus.ManagerApproved);
        return View("Pending", requests);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagerApprove(int id)
    {
        await _requestService.ManagerApproveAsync(id, GetCurrentUserId());
        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HRApprove(int id)
    {
        await _requestService.HRApproveAsync(id, GetCurrentUserId());
        return RedirectToAction(nameof(PendingHR));
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        await _requestService.RejectAsync(id, GetCurrentUserId());
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
