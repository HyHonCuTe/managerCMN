using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;
using managerCMN.Helpers;
using System.ComponentModel.DataAnnotations;

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
        await _contractService.FixEmptyContractNumbersAsync();
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
        if (model.ContractType == ContractType.Indefinite)
        {
            model.EndDate = null;
        }
        else if (model.ContractType == ContractType.FixedTerm
                 && model.SelectedDurationYears is 1 or 3)
        {
            model.EndDate = CalculateContractEndDate(model.StartDate, model.SelectedDurationYears.Value);
        }

        if (!ModelState.IsValid)
        {
            await PopulateEmployees();
            return View(model);
        }

        // Validate ContractNumber uniqueness
        if (!await _contractService.IsContractNumberUniqueAsync(model.ContractNumber))
        {
            ModelState.AddModelError("ContractNumber", "Số hợp đồng này đã tồn tại.");
            await PopulateEmployees();
            return View(model);
        }

        // Additional server-side file validation
        if (model.ContractFile != null)
        {
            var validationResult = FileUploadHelper.ValidateFile(
                model.ContractFile,
                FileUploadHelper.AllowedDocumentExtensions,
                false);

            if (validationResult != ValidationResult.Success)
            {
                ModelState.AddModelError("ContractFile", validationResult.ErrorMessage ?? "File không hợp lệ.");
                await PopulateEmployees();
                return View(model);
            }
        }

        string? filePath = null;
        if (model.ContractFile != null)
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
            Directory.CreateDirectory(uploadsDir);

            // Use secure filename generation
            var secureFileName = FileUploadHelper.GenerateSecureFileName(model.ContractFile.FileName, "contract");
            var fullPath = Path.Combine(uploadsDir, secureFileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await model.ContractFile.CopyToAsync(stream);
            filePath = $"/uploads/contracts/{secureFileName}";
        }

        var contract = new Contract
        {
            ContractNumber = model.ContractNumber.Trim(),
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
            ContractNumber = contract.ContractNumber,
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

        // Validate ContractNumber uniqueness (excluding current contract)
        if (!await _contractService.IsContractNumberUniqueAsync(model.ContractNumber, model.ContractId))
        {
            ModelState.AddModelError("ContractNumber", "Số hợp đồng này đã tồn tại.");
            await PopulateEmployees();
            return View(model);
        }

        var contract = await _contractService.GetByIdAsync(model.ContractId);
        if (contract == null) return NotFound();

        contract.ContractNumber = model.ContractNumber.Trim();
        contract.EmployeeId   = model.EmployeeId;
        contract.ContractType = model.ContractType;
        contract.StartDate    = model.StartDate;
        contract.EndDate      = model.EndDate;
        contract.Salary       = model.Salary;
        contract.Status       = model.Status;

        await _contractService.UpdateAsync(contract);
        TempData["Success"] = "Đã cập nhật hợp đồng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var contract = await _contractService.GetByIdAsync(id);
            if (contract == null)
            {
                TempData["Error"] = "Không tìm thấy hợp đồng cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            // Delete associated file if exists
            if (!string.IsNullOrEmpty(contract.FilePath))
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contract.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            await _contractService.DeleteAsync(id);
            TempData["Success"] = "Đã xóa hợp đồng thành công!";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi xóa hợp đồng: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateEmployees()
    {
        var employees = await _employeeService.GetAllAsync();
        ViewBag.Employees = new SelectList(employees, "EmployeeId", "FullName");
    }

    private static DateTime CalculateContractEndDate(DateTime startDate, int years)
        => startDate.Date.AddYears(years).AddDays(-1);

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("HopDong");

        string[] headers = {
            "Mã NV*",
            "Số HĐ*",
            "Loại HĐ (Thử việc/Xác định thời hạn/Không xác định thời hạn/Thời vụ)*",
            "Ngày bắt đầu (dd/MM/yyyy)*",
            "Ngày kết thúc (dd/MM/yyyy)",
            "Lương"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Sample data row
        ws.Cell(2, 1).Value = "NV001";
        ws.Cell(2, 2).Value = "001/2026/HĐLĐ-CMN";
        ws.Cell(2, 3).Value = "Thử việc";
        ws.Cell(2, 4).Value = "01/01/2026";
        ws.Cell(2, 5).Value = "31/12/2026";
        ws.Cell(2, 6).Value = "0";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "mau_nhap_hopdong.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        try
        {
            // Validate file
            var validationResult = FileUploadHelper.ValidateFile(
                file, FileUploadHelper.AllowedExcelExtensions, true);

            if (validationResult != ValidationResult.Success)
            {
                TempData["ImportError"] = validationResult.ErrorMessage ?? "File không hợp lệ.";
                return RedirectToAction(nameof(Create));
            }

            // Load employees for FK lookup
            var employees = (await _employeeService.GetAllAsync()).ToList();
            var employeeByCode = employees
                .Where(e => !string.IsNullOrEmpty(e.EmployeeCode))
                .ToDictionary(e => e.EmployeeCode.ToLower(), e => e);

            var errors = new List<string>();
            var toCreate = new List<Contract>();

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheets.First();
            int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var empCode = ws.Cell(row, 1).GetString().Trim();
                var contractNumberStr = ws.Cell(row, 2).GetString().Trim();
                var contractTypeStr = ws.Cell(row, 3).GetString().Trim();
                var startDateStr = ws.Cell(row, 4).GetString().Trim();
                var endDateStr = ws.Cell(row, 5).GetString().Trim();
                var salaryStr = ws.Cell(row, 6).GetString().Trim();

                // Skip empty rows
                if (string.IsNullOrEmpty(empCode) && string.IsNullOrEmpty(contractTypeStr))
                    continue;

                // Validate employee code
                if (string.IsNullOrEmpty(empCode))
                {
                    errors.Add($"Dòng {row}: Thiếu Mã NV.");
                    continue;
                }

                if (!employeeByCode.TryGetValue(empCode.ToLower(), out var employee))
                {
                    errors.Add($"Dòng {row}: Không tìm thấy nhân viên với mã '{empCode}'.");
                    continue;
                }

                // Validate contract number
                if (string.IsNullOrEmpty(contractNumberStr))
                {
                    errors.Add($"Dòng {row}: Thiếu số hợp đồng.");
                    continue;
                }

                // Check contract number uniqueness
                if (!await _contractService.IsContractNumberUniqueAsync(contractNumberStr))
                {
                    errors.Add($"Dòng {row}: Số hợp đồng '{contractNumberStr}' đã tồn tại.");
                    continue;
                }

                // Parse ContractType enum
                var contractType = ParseContractType(contractTypeStr);
                if (!contractType.HasValue)
                {
                    errors.Add($"Dòng {row}: Loại hợp đồng '{contractTypeStr}' không hợp lệ. Vui lòng dùng: Thử việc, Xác định thời hạn, Không xác định thời hạn, hoặc Thời vụ.");
                    continue;
                }

                // Parse start date
                var startDate = ParseDate(startDateStr);
                if (!startDate.HasValue)
                {
                    errors.Add($"Dòng {row}: Ngày bắt đầu '{startDateStr}' không hợp lệ. Định dạng: dd/MM/yyyy.");
                    continue;
                }

                // Parse end date (optional)
                DateTime? endDate = null;
                if (!string.IsNullOrEmpty(endDateStr))
                {
                    endDate = ParseDate(endDateStr);
                    if (!endDate.HasValue)
                    {
                        errors.Add($"Dòng {row}: Ngày kết thúc '{endDateStr}' không hợp lệ. Định dạng: dd/MM/yyyy.");
                        continue;
                    }

                    if (endDate.Value <= startDate.Value)
                    {
                        errors.Add($"Dòng {row}: Ngày kết thúc phải sau ngày bắt đầu.");
                        continue;
                    }
                }

                // Parse salary (default to 0)
                decimal salary = 0;
                if (!string.IsNullOrEmpty(salaryStr))
                {
                    if (!decimal.TryParse(salaryStr, out salary) || salary < 0)
                    {
                        errors.Add($"Dòng {row}: Lương '{salaryStr}' không hợp lệ. Phải là số >= 0.");
                        continue;
                    }
                }

                toCreate.Add(new Contract
                {
                    ContractNumber = contractNumberStr.Trim(),
                    EmployeeId = employee.EmployeeId,
                    ContractType = contractType.Value,
                    StartDate = startDate.Value,
                    EndDate = endDate,
                    Salary = salary,
                    Status = ContractStatus.Active
                });
            }

            if (errors.Any())
            {
                TempData["ImportErrors"] = string.Join("|", errors);
            }

            if (toCreate.Any())
            {
                try
                {
                    foreach (var contract in toCreate)
                    {
                        await _contractService.CreateAsync(contract);
                    }

                    TempData["ImportSuccess"] = $"Đã nhập thành công {toCreate.Count} hợp đồng.";
                }
                catch (DbUpdateException dbEx)
                {
                    TempData["Error"] = "Lỗi khi lưu dữ liệu: " +
                        (dbEx.InnerException?.Message ?? dbEx.Message) +
                        ". Vui lòng kiểm tra dữ liệu và thử lại.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception)
        {
            TempData["Error"] = "Lỗi nghiêm trọng khi xử lý file. Vui lòng kiểm tra định dạng file và thử lại.";
            return RedirectToAction(nameof(Create));
        }
    }

    private static DateTime? ParseDate(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        if (DateTime.TryParseExact(s, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d)) return d;
        if (DateTime.TryParse(s, out d)) return d;
        return null;
    }

    private static ContractType? ParseContractType(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        var normalized = s.Trim().ToLowerInvariant();

        return normalized switch
        {
            "thử việc" or "thu viec" => ContractType.Probation,
            "xác định thời hạn" or "xac dinh thoi han" or "có thời hạn" or "co thoi han" => ContractType.FixedTerm,
            "không xác định thời hạn" or "khong xac dinh thoi han" or "vô thời hạn" or "vo thoi han" => ContractType.Indefinite,
            "thời vụ" or "thoi vu" => ContractType.Seasonal,
            _ => null
        };
    }
}
