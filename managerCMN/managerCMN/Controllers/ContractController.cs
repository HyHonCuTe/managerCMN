using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class ContractController : Controller
{
    private readonly IContractService _contractService;
    private readonly IEmployeeService _employeeService;

    public ContractController(IContractService contractService, IEmployeeService employeeService)
    {
        _contractService = contractService;
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index()
    {
        var contracts = await _contractService.GetAllAsync();
        return View(contracts);
    }

    public async Task<IActionResult> Expiring()
    {
        var contracts = await _contractService.GetExpiringContractsAsync();
        return View(contracts);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateEmployees();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContractCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEmployees();
            return View(model);
        }

        string? filePath = null;
        if (model.ContractFile != null)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ContractFile.FileName)}";
            var fullPath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(fullPath, FileMode.Create);
            await model.ContractFile.CopyToAsync(stream);
            filePath = $"/uploads/contracts/{fileName}";
        }

        var contract = new Contract
        {
            EmployeeId = model.EmployeeId,
            ContractType = model.ContractType,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            Salary = model.Salary,
            FilePath = filePath
        };

        await _contractService.CreateAsync(contract);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var contract = await _contractService.GetByIdAsync(id);
        if (contract == null) return NotFound();
        await PopulateEmployees();
        return View(contract);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Contract contract)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEmployees();
            return View(contract);
        }

        await _contractService.UpdateAsync(contract);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateEmployees()
    {
        var employees = await _employeeService.GetAllAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeId", "FullName");
    }
}
