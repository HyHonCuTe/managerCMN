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
        await _contractService.SyncExpiredAsync();
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
        var vm = new ContractEditViewModel
        {
            ContractId   = contract.ContractId,
            EmployeeId   = contract.EmployeeId,
            ContractType = contract.ContractType,
            StartDate    = contract.StartDate,
            EndDate      = contract.EndDate,
            Salary       = contract.Salary,
            Status       = contract.Status
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ContractEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateEmployees();
            return View(model);
        }

        var contract = await _contractService.GetByIdAsync(model.ContractId);
        if (contract == null) return NotFound();

        contract.EmployeeId   = model.EmployeeId;
        contract.ContractType = model.ContractType;
        contract.StartDate    = model.StartDate;
        contract.EndDate      = model.EndDate;
        contract.Salary       = model.Salary;
        contract.Status       = model.Status;

        await _contractService.UpdateAsync(contract);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateEmployees()
    {
        var employees = await _employeeService.GetAllAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeId", "FullName");
    }
}
