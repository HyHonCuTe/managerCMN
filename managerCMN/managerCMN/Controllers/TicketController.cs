using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize]
public class TicketController : Controller
{
    private readonly ITicketService _ticketService;
    private readonly IEmployeeService _employeeService;

    public TicketController(ITicketService ticketService, IEmployeeService employeeService)
    {
        _ticketService = ticketService;
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index()
    {
        var tickets = User.IsInRole("Admin")
            ? await _ticketService.GetAllAsync()
            : await _ticketService.GetByCreatorAsync(GetCurrentEmployeeId());
        return View(tickets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        return View(ticket);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ticket = new Ticket
        {
            Title = model.Title,
            Description = model.Description,
            Priority = model.Priority,
            CreatedBy = GetCurrentEmployeeId()
        };

        await _ticketService.CreateAsync(ticket);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Assign(int id)
    {
        var ticket = await _ticketService.GetByIdAsync(id);
        if (ticket == null) return NotFound();

        var employees = await _employeeService.GetAllAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeId", "FullName");
        return View(ticket);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int assigneeId)
    {
        await _ticketService.AssignAsync(id, assigneeId);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, string resolution)
    {
        await _ticketService.ResolveAsync(id, resolution);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        await _ticketService.CloseAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
