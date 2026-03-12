using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;

    public EmployeeController(IEmployeeService employeeService, IDepartmentService departmentService)
    {
        _employeeService = employeeService;
        _departmentService = departmentService;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _employeeService.GetAllAsync();
        return View(employees);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _employeeService.GetWithDetailsAsync(id);
        if (employee == null) return NotFound();
        return View(employee);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDepartments();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartments();
            return View(model);
        }

        var employee = new Employee
        {
            FullName = model.FullName,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            Email = model.Email,
            Phone = model.Phone,
            PermanentAddress = model.PermanentAddress,
            TemporaryAddress = model.TemporaryAddress,
            TaxCode = model.TaxCode,
            BankAccount = model.BankAccount,
            BankName = model.BankName,
            DepartmentId = model.DepartmentId,
            Position = model.Position,
            Qualifications = model.Qualifications,
            StartWorkingDate = model.StartWorkingDate
        };

        await _employeeService.CreateAsync(employee);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee == null) return NotFound();

        await PopulateDepartments();
        var model = new EmployeeEditViewModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            DateOfBirth = employee.DateOfBirth,
            Gender = employee.Gender,
            Email = employee.Email,
            Phone = employee.Phone,
            PermanentAddress = employee.PermanentAddress,
            TemporaryAddress = employee.TemporaryAddress,
            TaxCode = employee.TaxCode,
            BankAccount = employee.BankAccount,
            BankName = employee.BankName,
            DepartmentId = employee.DepartmentId,
            Position = employee.Position,
            Qualifications = employee.Qualifications,
            StartWorkingDate = employee.StartWorkingDate,
            Status = employee.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDepartments();
            return View(model);
        }

        var employee = await _employeeService.GetByIdAsync(model.EmployeeId);
        if (employee == null) return NotFound();

        employee.FullName = model.FullName;
        employee.DateOfBirth = model.DateOfBirth;
        employee.Gender = model.Gender;
        employee.Email = model.Email;
        employee.Phone = model.Phone;
        employee.PermanentAddress = model.PermanentAddress;
        employee.TemporaryAddress = model.TemporaryAddress;
        employee.TaxCode = model.TaxCode;
        employee.BankAccount = model.BankAccount;
        employee.BankName = model.BankName;
        employee.DepartmentId = model.DepartmentId;
        employee.Position = model.Position;
        employee.Qualifications = model.Qualifications;
        employee.StartWorkingDate = model.StartWorkingDate;
        employee.Status = model.Status;

        await _employeeService.UpdateAsync(employee);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _employeeService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDepartments()
    {
        var departments = await _departmentService.GetAllAsync();
        ViewBag.Departments = new SelectList(departments, "DepartmentId", "DepartmentName");
    }
}
