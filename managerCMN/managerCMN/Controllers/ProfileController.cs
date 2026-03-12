using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "Authenticated")]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationService _notificationService;

    public ProfileController(ApplicationDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    public async Task<IActionResult> Index()
    {
        var empId = GetEmployeeId();
        if (empId == null)
        {
            ViewBag.NoEmployee = true;
            return View("Index", null);
        }

        var employee = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.EmergencyContacts)
            .FirstOrDefaultAsync(e => e.EmployeeId == empId.Value);

        if (employee == null)
        {
            ViewBag.NoEmployee = true;
            return View("Index", null);
        }

        return View(employee);
    }

    public async Task<IActionResult> Edit()
    {
        var empId = GetEmployeeId();
        if (empId == null) return RedirectToAction("Index", "Dashboard");

        var employee = await _db.Employees.FindAsync(empId.Value);
        if (employee == null) return NotFound();

        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string? Phone,
        string? PermanentAddress,
        string? TemporaryAddress,
        string? BankAccount,
        string? BankName,
        string? TaxCode)
    {
        var empId = GetEmployeeId();
        if (empId == null) return RedirectToAction("Index", "Dashboard");

        var employee = await _db.Employees.FindAsync(empId.Value);
        if (employee == null) return NotFound();

        var changes = new List<string>();
        if (employee.Phone != Phone) changes.Add($"SĐT: {employee.Phone} → {Phone}");
        if (employee.PermanentAddress != PermanentAddress) changes.Add("Địa chỉ thường trú đã thay đổi");
        if (employee.TemporaryAddress != TemporaryAddress) changes.Add("Địa chỉ tạm trú đã thay đổi");
        if (employee.BankAccount != BankAccount) changes.Add($"Tài khoản ngân hàng: {employee.BankAccount} → {BankAccount}");
        if (employee.BankName != BankName) changes.Add($"Ngân hàng: {employee.BankName} → {BankName}");
        if (employee.TaxCode != TaxCode) changes.Add($"MST: {employee.TaxCode} → {TaxCode}");

        employee.Phone = Phone;
        employee.PermanentAddress = PermanentAddress;
        employee.TemporaryAddress = TemporaryAddress;
        employee.BankAccount = BankAccount;
        employee.BankName = BankName;
        employee.TaxCode = TaxCode;

        await _db.SaveChangesAsync();

        // Notify admin & manager users about the change
        if (changes.Count > 0)
        {
            var title = $"Nhân viên {employee.FullName} ({employee.EmployeeCode}) đã cập nhật thông tin";
            var message = string.Join("; ", changes);

            var adminManagerUserIds = await _db.Set<Models.Entities.UserRole>()
                .Where(ur => ur.RoleId == 1 || ur.RoleId == 2)
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var uid in adminManagerUserIds)
            {
                await _notificationService.CreateAsync(uid, title, message);
            }
        }

        TempData["Success"] = "Cập nhật thông tin thành công!";
        return RedirectToAction(nameof(Index));
    }
}
