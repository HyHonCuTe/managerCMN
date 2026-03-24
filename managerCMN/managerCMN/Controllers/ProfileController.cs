using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Services.Interfaces;
using managerCMN.Models.Entities;

namespace managerCMN.Controllers;

public class EmergencyContactDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Relationship { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

[Authorize(Policy = "Authenticated")]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILeaveService _leaveService;
    private readonly INotificationService _notificationService;

    public ProfileController(ApplicationDbContext db, ILeaveService leaveService, INotificationService notificationService)
    {
        _db = db;
        _leaveService = leaveService;
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
            .Include(e => e.JobTitle)
            .Include(e => e.EmergencyContacts)
            .Include(e => e.Contracts)
            .FirstOrDefaultAsync(e => e.EmployeeId == empId.Value);

        if (employee == null)
        {
            ViewBag.NoEmployee = true;
            return View("Index", null);
        }

        ViewBag.LeaveSummary = await _leaveService.GetBalanceSummaryAsync(employee.EmployeeId);

        return View(employee);
    }

    public async Task<IActionResult> Edit()
    {
        var empId = GetEmployeeId();
        if (empId == null) return RedirectToAction("Index", "Dashboard");

        var employee = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .Include(e => e.JobTitle)
            .Include(e => e.EmergencyContacts)
            .FirstOrDefaultAsync(e => e.EmployeeId == empId.Value);
        if (employee == null) return NotFound();

        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string? FullName,
        DateTime? DateOfBirth,
        int Gender,
        string? Ethnicity,
        string? Nationality,
        string? Phone,
        string? Qualifications,
        string? IdCardNumber,
        DateTime? IdCardIssueDate,
        string? IdCardIssuePlace,
        string? PermanentAddress,
        string? TemporaryAddress,
        string? TaxCode,
        string? BankAccount,
        string? BankName,
        string? InsuranceCode,
        string? VehiclePlate,
        string? FacebookUrl,
        List<EmergencyContactDto>? EmergencyContacts)
    {
        var empId = GetEmployeeId();
        if (empId == null) return RedirectToAction("Index", "Dashboard");

        // Validate TaxCode if provided
        if (!string.IsNullOrWhiteSpace(TaxCode))
        {
            TaxCode = TaxCode.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(TaxCode, @"^[0-9]{12}$"))
            {
                TempData["Error"] = "Mã số thuế phải là 12 chữ số.";
                var emp = await _db.Employees
                    .Include(e => e.Department)
                    .Include(e => e.JobTitle)
                    .Include(e => e.Position)
                    .Include(e => e.EmergencyContacts)
                    .FirstOrDefaultAsync(e => e.EmployeeId == empId.Value);
                return View(emp);
            }
        }

        var employee = await _db.Employees
            .Include(e => e.EmergencyContacts)
            .FirstOrDefaultAsync(e => e.EmployeeId == empId.Value);
        if (employee == null) return NotFound();

        // Track changes for notification
        var changes = new List<string>();

        // Personal Information
        if (employee.FullName != FullName?.Trim())
        {
            changes.Add($"Họ tên: {employee.FullName} → {FullName?.Trim()}");
            employee.FullName = FullName?.Trim() ?? employee.FullName;
        }

        if (employee.DateOfBirth != DateOfBirth)
        {
            changes.Add($"Ngày sinh: {employee.DateOfBirth?.ToString("dd/MM/yyyy")} → {DateOfBirth?.ToString("dd/MM/yyyy")}");
            employee.DateOfBirth = DateOfBirth;
        }

        if ((int)employee.Gender != Gender)
        {
            changes.Add($"Giới tính đã thay đổi");
            employee.Gender = (managerCMN.Models.Enums.Gender)Gender;
        }

        if (employee.Ethnicity != Ethnicity?.Trim())
        {
            changes.Add($"Dân tộc: {employee.Ethnicity} → {Ethnicity?.Trim()}");
            employee.Ethnicity = Ethnicity?.Trim();
        }

        if (employee.Nationality != Nationality?.Trim())
        {
            changes.Add($"Quốc tịch: {employee.Nationality} → {Nationality?.Trim()}");
            employee.Nationality = Nationality?.Trim();
        }

        if (employee.Phone != Phone?.Trim())
        {
            changes.Add($"SĐT: {employee.Phone} → {Phone?.Trim()}");
            employee.Phone = Phone?.Trim();
        }

        if (employee.Qualifications != Qualifications?.Trim())
        {
            changes.Add($"Bằng cấp đã cập nhật");
            employee.Qualifications = Qualifications?.Trim();
        }

        // ID Card Information
        if (employee.IdCardNumber != IdCardNumber?.Trim())
        {
            changes.Add($"Số CCCD: {employee.IdCardNumber} → {IdCardNumber?.Trim()}");
            employee.IdCardNumber = IdCardNumber?.Trim();
        }

        if (employee.IdCardIssueDate != IdCardIssueDate)
        {
            changes.Add($"Ngày cấp CCCD đã cập nhật");
            employee.IdCardIssueDate = IdCardIssueDate;
        }

        if (employee.IdCardIssuePlace != IdCardIssuePlace?.Trim())
        {
            changes.Add($"Nơi cấp CCCD: {employee.IdCardIssuePlace} → {IdCardIssuePlace?.Trim()}");
            employee.IdCardIssuePlace = IdCardIssuePlace?.Trim();
        }

        // Address
        if (employee.PermanentAddress != PermanentAddress?.Trim())
        {
            changes.Add("Địa chỉ thường trú đã thay đổi");
            employee.PermanentAddress = PermanentAddress?.Trim();
        }

        if (employee.TemporaryAddress != TemporaryAddress?.Trim())
        {
            changes.Add("Địa chỉ liên hệ đã thay đổi");
            employee.TemporaryAddress = TemporaryAddress?.Trim();
        }

        // Financial & Insurance
        if (employee.TaxCode != TaxCode?.Trim())
        {
            changes.Add($"MST: {employee.TaxCode} → {TaxCode?.Trim()}");
            employee.TaxCode = TaxCode?.Trim();
        }

        if (employee.BankAccount != BankAccount?.Trim())
        {
            changes.Add($"Tài khoản ngân hàng: {employee.BankAccount} → {BankAccount?.Trim()}");
            employee.BankAccount = BankAccount?.Trim();
        }

        if (employee.BankName != BankName?.Trim())
        {
            changes.Add($"Ngân hàng: {employee.BankName} → {BankName?.Trim()}");
            employee.BankName = BankName?.Trim();
        }

        if (employee.InsuranceCode != InsuranceCode?.Trim())
        {
            changes.Add($"Mã BHXH: {employee.InsuranceCode} → {InsuranceCode?.Trim()}");
            employee.InsuranceCode = InsuranceCode?.Trim();
        }

        // Other
        if (employee.VehiclePlate != VehiclePlate?.Trim())
        {
            changes.Add($"Số xe: {employee.VehiclePlate} → {VehiclePlate?.Trim()}");
            employee.VehiclePlate = VehiclePlate?.Trim();
        }

        if (employee.FacebookUrl != FacebookUrl?.Trim())
        {
            changes.Add($"Facebook URL đã cập nhật");
            employee.FacebookUrl = FacebookUrl?.Trim();
        }

        // Emergency Contacts
        if (EmergencyContacts != null)
        {
            var submittedIds = EmergencyContacts
                .Where(c => c.Id > 0)
                .Select(c => c.Id)
                .ToList();

            // Remove contacts not in the submitted list
            var contactsToRemove = employee.EmergencyContacts
                .Where(ec => !submittedIds.Contains(ec.Id))
                .ToList();

            foreach (var contact in contactsToRemove)
            {
                _db.EmployeeContacts.Remove(contact);
                changes.Add($"Đã xóa người liên hệ: {contact.FullName}");
            }

            // Add or update contacts
            foreach (var dto in EmergencyContacts.Where(c => !string.IsNullOrWhiteSpace(c.FullName)))
            {
                if (dto.Id > 0)
                {
                    // Update existing
                    var existing = employee.EmergencyContacts.FirstOrDefault(ec => ec.Id == dto.Id);
                    if (existing != null)
                    {
                        bool contactChanged = false;
                        if (existing.FullName != dto.FullName?.Trim())
                        {
                            existing.FullName = dto.FullName?.Trim() ?? existing.FullName;
                            contactChanged = true;
                        }
                        if (existing.Relationship != dto.Relationship?.Trim())
                        {
                            existing.Relationship = dto.Relationship?.Trim();
                            contactChanged = true;
                        }
                        if (existing.Phone != dto.Phone?.Trim())
                        {
                            existing.Phone = dto.Phone?.Trim();
                            contactChanged = true;
                        }
                        if (existing.Address != dto.Address?.Trim())
                        {
                            existing.Address = dto.Address?.Trim();
                            contactChanged = true;
                        }

                        if (contactChanged)
                        {
                            changes.Add($"Đã cập nhật người liên hệ: {existing.FullName}");
                        }
                    }
                }
                else
                {
                    // Add new
                    var newContact = new EmployeeContact
                    {
                        EmployeeId = employee.EmployeeId,
                        FullName = dto.FullName!.Trim(),
                        Relationship = dto.Relationship?.Trim(),
                        Phone = dto.Phone?.Trim(),
                        Address = dto.Address?.Trim()
                    };
                    _db.EmployeeContacts.Add(newContact);
                    changes.Add($"Đã thêm người liên hệ: {newContact.FullName}");
                }
            }
        }

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

        TempData["Success"] = changes.Count > 0
            ? $"Đã cập nhật {changes.Count} thông tin thành công!"
            : "Không có thay đổi nào được thực hiện.";
        return RedirectToAction(nameof(Index));
    }
}
