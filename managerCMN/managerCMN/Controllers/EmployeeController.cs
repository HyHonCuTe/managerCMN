using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;
using managerCMN.Data;
using managerCMN.Helpers;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace managerCMN.Controllers;

[Authorize(Policy = "ManagerOrAdmin")]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly ILeaveService _leaveService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, IDepartmentService departmentService, ILeaveService leaveService, ApplicationDbContext db, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _departmentService = departmentService;
        _leaveService = leaveService;
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var employees = (await _employeeService.GetAllAsync()).ToList();
        ViewBag.LeaveSummaries = await _leaveService.GetBalanceSummariesAsync(employees.Select(e => e.EmployeeId));
        ViewBag.Departments = (await _departmentService.GetAllAsync())
            .Select(d => d.DepartmentName).OrderBy(n => n).ToList();
        return View(employees);
    }

    public async Task<IActionResult> Details(int id)
    {
        var employee = await _employeeService.GetWithDetailsAsync(id);
        if (employee == null) return NotFound();

        ViewBag.LeaveSummary = await _leaveService.GetBalanceSummaryAsync(id);
        ViewBag.LeaveRequests = (await _leaveService.GetRequestsByEmployeeAsync(id)).Take(10).ToList();
        return View(employee);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        var employee = new Employee
        {
            EmployeeCode       = model.EmployeeCode?.Trim() ?? "",
            FullName           = model.FullName,
            DateOfBirth        = model.DateOfBirth,
            Gender             = model.Gender,
            Email              = model.Email,
            Phone              = model.Phone,
            AttendanceName     = model.AttendanceName,
            AttendanceCode     = model.AttendanceCode,
            PermanentAddress   = model.PermanentAddress,
            TemporaryAddress   = model.TemporaryAddress,
            TaxCode            = model.TaxCode,
            BankAccount        = model.BankAccount,
            BankName           = model.BankName,
            DepartmentId       = model.DepartmentId,
            JobTitleId         = model.JobTitleId,
            PositionId         = model.PositionId,
            Ethnicity          = model.Ethnicity,
            Nationality        = model.Nationality,
            IdCardNumber       = model.IdCardNumber,
            IdCardIssueDate    = model.IdCardIssueDate,
            IdCardIssuePlace   = model.IdCardIssuePlace,
            Qualifications     = model.Qualifications,
            StartWorkingDate   = model.StartWorkingDate,
            InsuranceCode      = model.InsuranceCode,
            VehiclePlate       = model.VehiclePlate,
            FacebookUrl        = model.FacebookUrl
        };

        await _employeeService.CreateAsync(employee);

        foreach (var c in model.EmergencyContacts.Where(c => !string.IsNullOrWhiteSpace(c.FullName)))
        {
            _db.EmployeeContacts.Add(new EmployeeContact
            {
                EmployeeId   = employee.EmployeeId,
                FullName     = c.FullName!,
                Relationship = c.Relationship,
                Phone        = c.Phone,
                Address      = c.Address
            });
        }
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee == null) return NotFound();

        await PopulateDropdowns();
        var model = new EmployeeEditViewModel
        {
            EmployeeId         = employee.EmployeeId,
            EmployeeCode       = employee.EmployeeCode,
            FullName           = employee.FullName,
            DateOfBirth        = employee.DateOfBirth,
            Gender             = employee.Gender,
            Email              = employee.Email,
            Phone              = employee.Phone,
            AttendanceName     = employee.AttendanceName,
            AttendanceCode     = employee.AttendanceCode,
            PermanentAddress   = employee.PermanentAddress,
            TemporaryAddress   = employee.TemporaryAddress,
            TaxCode            = employee.TaxCode,
            BankAccount        = employee.BankAccount,
            BankName           = employee.BankName,
            DepartmentId       = employee.DepartmentId,
            JobTitleId         = employee.JobTitleId,
            PositionId         = employee.PositionId,
            Ethnicity          = employee.Ethnicity,
            Nationality        = employee.Nationality,
            IdCardNumber       = employee.IdCardNumber,
            IdCardIssueDate    = employee.IdCardIssueDate,
            IdCardIssuePlace   = employee.IdCardIssuePlace,
            Qualifications     = employee.Qualifications,
            StartWorkingDate   = employee.StartWorkingDate,
            InsuranceCode      = employee.InsuranceCode,
            ResignationDate    = employee.ResignationDate,
            ResignationReason  = employee.ResignationReason,
            VehiclePlate       = employee.VehiclePlate,
            FacebookUrl        = employee.FacebookUrl,
            Status             = employee.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        var employee = await _employeeService.GetByIdAsync(model.EmployeeId);
        if (employee == null) return NotFound();

        employee.FullName           = model.FullName;
        employee.DateOfBirth        = model.DateOfBirth;
        employee.Gender             = model.Gender;
        employee.Email              = model.Email;
        employee.Phone              = model.Phone;
        employee.AttendanceName     = model.AttendanceName;
        employee.AttendanceCode     = model.AttendanceCode;
        employee.PermanentAddress   = model.PermanentAddress;
        employee.TemporaryAddress   = model.TemporaryAddress;
        employee.TaxCode            = model.TaxCode;
        employee.BankAccount        = model.BankAccount;
        employee.BankName           = model.BankName;
        employee.DepartmentId       = model.DepartmentId;
        employee.JobTitleId         = model.JobTitleId;
        employee.PositionId         = model.PositionId;
        employee.Ethnicity          = model.Ethnicity;
        employee.Nationality        = model.Nationality;
        employee.IdCardNumber       = model.IdCardNumber;
        employee.IdCardIssueDate    = model.IdCardIssueDate;
        employee.IdCardIssuePlace   = model.IdCardIssuePlace;
        employee.Qualifications     = model.Qualifications;
        employee.StartWorkingDate   = model.StartWorkingDate;
        employee.InsuranceCode      = model.InsuranceCode;
        employee.ResignationDate    = model.ResignationDate;
        employee.ResignationReason  = model.ResignationReason;
        employee.VehiclePlate       = model.VehiclePlate;
        employee.FacebookUrl        = model.FacebookUrl;
        employee.Status             = model.Status;

        await _employeeService.UpdateAsync(employee);
        TempData["Success"] = "Đã cập nhật thông tin nhân viên thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _employeeService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustLeaveBalance(int id, decimal currentYearAdjustment, decimal carryForwardAdjustment)
    {
        try
        {
            // Log parameters for debugging
            Console.WriteLine($"AdjustLeaveBalance called: EmployeeId={id}, CurrentYear={currentYearAdjustment}, CarryForward={carryForwardAdjustment}");

            await _leaveService.AdjustBalanceAsync(id, DateTime.Today.Year, currentYearAdjustment, carryForwardAdjustment);
            TempData["Success"] = "Đã cập nhật số phép cho nhân viên.";
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"InvalidOperationException in AdjustLeaveBalance: {ex.Message}");
            TempData["Error"] = ex.Message;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in AdjustLeaveBalance: {ex.Message} - Stack: {ex.StackTrace}");
            TempData["Error"] = "Có lỗi xảy ra khi cập nhật số phép: " + ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("NhanVien");
        string[] headers = {
            "Mã NV", "Họ tên*", "Email*", "SDT", "Ngày sinh (dd/MM/yyyy)",
            "Giới tính (Nam/Nữ/Khác)", "Dân tộc", "Quốc tịch",
            "Chấm công - Tên", "Chấm công - Mã số",
            "Phòng ban", "Chức vụ", "Vị trí", "Ngày vào làm (dd/MM/yyyy)",
            "CCCD Số", "CCCD Ngày cấp (dd/MM/yyyy)", "CCCD Nơi cấp",
            "Địa chỉ thường trú", "Địa chỉ liên hệ",
            "MST", "Số tài khoản", "Ngân hàng", "Bằng cấp",
            "Mã BHXH", "Số xe", "Facebook URL",
            "NThân1 - Họ tên", "NThân1 - Quan hệ", "NThân1 - SĐT", "NThân1 - Địa chỉ",
            "NThân2 - Họ tên", "NThân2 - Quan hệ", "NThân2 - SĐT", "NThân2 - Địa chỉ",
            "NThân3 - Họ tên", "NThân3 - Quan hệ", "NThân3 - SĐT", "NThân3 - Địa chỉ"
        };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "mau_nhap_nhanvien.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        _logger.LogInformation("Starting employee import. File: {FileName}, Size: {FileSize}", file.FileName, file.Length);

        try
        {
            // Validate using FileUploadHelper
            var validationResult = FileUploadHelper.ValidateFile(
                file,
                FileUploadHelper.AllowedExcelExtensions,
                true);

            if (validationResult != ValidationResult.Success)
            {
                _logger.LogWarning("File validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                TempData["ImportError"] = validationResult.ErrorMessage ?? "File không hợp lệ.";
                return RedirectToAction(nameof(Create));
            }

            var departments = (await _departmentService.GetAllAsync()).ToList();
            var jobTitles   = await _db.JobTitles.Where(j => j.IsActive).ToListAsync();
            var positions   = await _db.Positions.Where(p => p.IsActive).ToListAsync();

            // Load existing employees for duplicate checking
            var existingEmployees = await _db.Employees
                .Select(e => new {
                    e.EmployeeCode,
                    e.Email,
                    e.AttendanceCode
                })
                .ToListAsync();

            var existingCodes = new HashSet<string>(
                existingEmployees.Select(e => e.EmployeeCode.ToLower()));
            var existingEmails = new HashSet<string>(
                existingEmployees.Select(e => e.Email.ToLower()));
            var existingAttendanceCodes = new HashSet<string>(
                existingEmployees.Where(e => e.AttendanceCode != null)
                    .Select(e => e.AttendanceCode!.ToLower()));

            // Track codes in current batch
            var batchCodes = new HashSet<string>();
            var batchEmails = new HashSet<string>();
            var batchAttendanceCodes = new HashSet<string>();

            var errors   = new List<string>();
            var toCreate = new List<(Employee Emp, List<(string Name, string? Rel, string? Phone, string? Addr)> Contacts)>();

            static DateTime? ParseDate(string s)
            {
                if (string.IsNullOrEmpty(s)) return null;
                if (DateTime.TryParseExact(s, "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var d)) return d;
                if (DateTime.TryParse(s, out d)) return d;
                return null;
            }
            static string? Trim(string s) => string.IsNullOrEmpty(s.Trim()) ? null : s.Trim();

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);
            var ws      = workbook.Worksheets.First();
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            _logger.LogInformation("Processing {RowCount} rows from Excel file", lastRow - 1);

            for (int row = 2; row <= lastRow; row++)
            {
                var empCode  = ws.Cell(row, 1).GetString().Trim();
                var fullName = ws.Cell(row, 2).GetString().Trim();
                var email    = ws.Cell(row, 3).GetString().Trim();

                if (string.IsNullOrEmpty(fullName) && string.IsNullOrEmpty(email)) continue;
                if (string.IsNullOrEmpty(fullName)) { errors.Add($"Dòng {row}: Thiếu Họ tên."); continue; }
                if (string.IsNullOrEmpty(email))    { errors.Add($"Dòng {row}: Thiếu Email.");    continue; }

                // Duplicate validation for EmployeeCode
                if (!string.IsNullOrEmpty(empCode))
                {
                    var empCodeLower = empCode.ToLower();
                    if (existingCodes.Contains(empCodeLower))
                    {
                        errors.Add($"Dòng {row}: Mã nhân viên '{empCode}' đã tồn tại trong hệ thống.");
                        continue;
                    }
                    if (batchCodes.Contains(empCodeLower))
                    {
                        errors.Add($"Dòng {row}: Mã nhân viên '{empCode}' bị trùng trong file import.");
                        continue;
                    }
                    batchCodes.Add(empCodeLower);
                }

                // Duplicate validation for Email
                var emailLower = email.ToLower();
                if (existingEmails.Contains(emailLower))
                {
                    errors.Add($"Dòng {row}: Email '{email}' đã tồn tại trong hệ thống.");
                    continue;
                }
                if (batchEmails.Contains(emailLower))
                {
                    errors.Add($"Dòng {row}: Email '{email}' bị trùng trong file import.");
                    continue;
                }
                batchEmails.Add(emailLower);

                // Duplicate validation for AttendanceCode
                var attendanceCode = Trim(ws.Cell(row, 10).GetString());
                if (!string.IsNullOrEmpty(attendanceCode))
                {
                    var attendanceCodeLower = attendanceCode.ToLower();
                    if (existingAttendanceCodes.Contains(attendanceCodeLower))
                    {
                        errors.Add($"Dòng {row}: Mã chấm công '{attendanceCode}' đã tồn tại trong hệ thống.");
                        continue;
                    }
                    if (batchAttendanceCodes.Contains(attendanceCodeLower))
                    {
                        errors.Add($"Dòng {row}: Mã chấm công '{attendanceCode}' bị trùng trong file import.");
                        continue;
                    }
                    batchAttendanceCodes.Add(attendanceCodeLower);
                }

                var genderStr = ws.Cell(row, 6).GetString().Trim().ToLowerInvariant();
                var gender = genderStr is "nữ" or "nu" ? Gender.Female
                           : genderStr is "khác" or "khac" ? Gender.Other
                           : Gender.Male;

                var deptName = ws.Cell(row, 11).GetString().Trim();
                var dept = departments.FirstOrDefault(d =>
                    d.DepartmentName.Equals(deptName, StringComparison.OrdinalIgnoreCase));

                var jtName = ws.Cell(row, 12).GetString().Trim();
                var jt = jobTitles.FirstOrDefault(j =>
                    j.JobTitleName.Equals(jtName, StringComparison.OrdinalIgnoreCase));

                var posName = ws.Cell(row, 13).GetString().Trim();
                var pos = positions.FirstOrDefault(p =>
                    p.PositionName.Equals(posName, StringComparison.OrdinalIgnoreCase));

                var emp = new Employee
                {
                    EmployeeCode       = string.IsNullOrEmpty(empCode) ? "" : empCode,
                    FullName           = fullName,
                    Email              = email,
                    Phone              = Trim(ws.Cell(row, 4).GetString()),
                    DateOfBirth        = ParseDate(ws.Cell(row, 5).GetString().Trim()),
                    Gender             = gender,
                    Ethnicity          = Trim(ws.Cell(row, 7).GetString()),
                    Nationality        = Trim(ws.Cell(row, 8).GetString()),
                    AttendanceName     = Trim(ws.Cell(row, 9).GetString()),
                    AttendanceCode     = attendanceCode,
                    DepartmentId       = dept?.DepartmentId,
                    JobTitleId         = jt?.JobTitleId,
                    PositionId         = pos?.PositionId,
                    StartWorkingDate   = ParseDate(ws.Cell(row, 14).GetString().Trim()),
                    IdCardNumber       = Trim(ws.Cell(row, 15).GetString()),
                    IdCardIssueDate    = ParseDate(ws.Cell(row, 16).GetString().Trim()),
                    IdCardIssuePlace   = Trim(ws.Cell(row, 17).GetString()),
                    PermanentAddress   = Trim(ws.Cell(row, 18).GetString()),
                    TemporaryAddress   = Trim(ws.Cell(row, 19).GetString()),
                    TaxCode            = Trim(ws.Cell(row, 20).GetString()),
                    BankAccount        = Trim(ws.Cell(row, 21).GetString()),
                    BankName           = Trim(ws.Cell(row, 22).GetString()),
                    Qualifications     = Trim(ws.Cell(row, 23).GetString()),
                    InsuranceCode      = Trim(ws.Cell(row, 24).GetString()),
                    VehiclePlate       = Trim(ws.Cell(row, 25).GetString()),
                    FacebookUrl        = Trim(ws.Cell(row, 26).GetString())
                };

                // Parse up to 3 emergency contacts (cols 27-38)
                var contacts = new List<(string Name, string? Rel, string? Phone, string? Addr)>();
                for (int block = 0; block < 3; block++)
                {
                    int col = 27 + block * 4;
                    var cName = ws.Cell(row, col).GetString().Trim();
                    if (!string.IsNullOrEmpty(cName))
                        contacts.Add((cName,
                            Trim(ws.Cell(row, col + 1).GetString()),
                            Trim(ws.Cell(row, col + 2).GetString()),
                            Trim(ws.Cell(row, col + 3).GetString())));
                }

                toCreate.Add((emp, contacts));
            }

            if (errors.Any())
            {
                _logger.LogWarning("Import completed with {ErrorCount} validation errors", errors.Count);
                TempData["ImportErrors"] = string.Join("|", errors);
            }

            if (toCreate.Any())
            {
                try
                {
                    foreach (var (emp, contacts) in toCreate)
                    {
                        await _employeeService.CreateAsync(emp);
                        foreach (var (name, rel, phone, addr) in contacts)
                        {
                            _db.EmployeeContacts.Add(new EmployeeContact
                            {
                                EmployeeId   = emp.EmployeeId,
                                FullName     = name,
                                Relationship = rel,
                                Phone        = phone,
                                Address      = addr
                            });
                        }
                    }
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Successfully imported {Count} employees", toCreate.Count);
                    TempData["ImportSuccess"] = $"Đã nhập thành công {toCreate.Count} nhân viên.";
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error during employee import. Inner exception: {InnerException}",
                        dbEx.InnerException?.Message);

                    TempData["Error"] = "Lỗi khi lưu dữ liệu: " +
                        (dbEx.InnerException?.Message ?? dbEx.Message) +
                        ". Vui lòng kiểm tra dữ liệu và thử lại.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during employee import");
                    TempData["Error"] = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during employee import process. File: {FileName}", file.FileName);
            TempData["Error"] = "Lỗi nghiêm trọng khi xử lý file. Vui lòng kiểm tra định dạng file và thử lại.";
            return RedirectToAction(nameof(Create));
        }
    }

    private async Task PopulateDropdowns()
    {
        var departments = await _departmentService.GetAllAsync();
        ViewBag.Departments = new SelectList(departments, "DepartmentId", "DepartmentName");
        var jobTitles = await _db.JobTitles.Where(j => j.IsActive).OrderBy(j => j.SortOrder).ToListAsync();
        ViewBag.JobTitles = new SelectList(jobTitles, "JobTitleId", "JobTitleName");
        var positions = await _db.Positions.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync();
        ViewBag.Positions = new SelectList(positions, "PositionId", "PositionName");
    }
}
