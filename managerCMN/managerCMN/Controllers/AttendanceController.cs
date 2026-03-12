using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
        => _attendanceService = attendanceService;

    public async Task<IActionResult> Index(int? year, int? month)
    {
        year ??= DateTime.UtcNow.Year;
        month ??= DateTime.UtcNow.Month;

        var startDate = new DateOnly(year.Value, month.Value, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var attendances = await _attendanceService.GetByDateRangeAsync(startDate, endDate);

        ViewBag.Year = year;
        ViewBag.Month = month;
        return View(attendances);
    }

    public async Task<IActionResult> LateReport(int? year, int? month)
    {
        year ??= DateTime.UtcNow.Year;
        month ??= DateTime.UtcNow.Month;

        var lateRecords = await _attendanceService.GetLateCheckInsAsync(year.Value, month.Value);

        ViewBag.Year = year;
        ViewBag.Month = month;
        return View(lateRecords);
    }

    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(AttendanceImportViewModel model)
    {
        if (model.ExcelFile == null || model.ExcelFile.Length == 0)
        {
            ModelState.AddModelError("", "Vui lòng chọn file Excel.");
            return View(model);
        }

        using var stream = model.ExcelFile.OpenReadStream();
        await _attendanceService.ImportFromExcelAsync(stream);

        TempData["Success"] = "Import chấm công thành công!";
        return RedirectToAction(nameof(Index));
    }
}
