using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;

namespace managerCMN.Controllers;

[Authorize(Policy = "Authenticated")]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    public AttendanceController(IAttendanceService attendanceService, IEmployeeService employeeService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
    }

    public async Task<IActionResult> Index(int? year, int? month, int? employeeId)
    {
        year ??= DateTime.Now.Year;
        month ??= DateTime.Now.Month;

        bool isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");
        int? myEmployeeId = null;
        var empIdClaim = User.FindFirst("EmployeeId");
        if (empIdClaim != null && int.TryParse(empIdClaim.Value, out var eid))
            myEmployeeId = eid;

        var employees = await _employeeService.GetAllAsync();
        List<EmployeeSelectItem> empList;

        if (isAdminOrManager)
        {
            empList = employees
                .OrderBy(e => e.Department?.DepartmentName).ThenBy(e => e.FullName)
                .Select(e => new EmployeeSelectItem
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.Department?.DepartmentName
                }).ToList();
            employeeId ??= empList.FirstOrDefault()?.EmployeeId;
        }
        else
        {
            // Regular user: only see their own attendance
            employeeId = myEmployeeId;
            empList = employees
                .Where(e => e.EmployeeId == myEmployeeId)
                .Select(e => new EmployeeSelectItem
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    DepartmentName = e.Department?.DepartmentName
                }).ToList();
        }

        var model = new AttendanceCalendarViewModel
        {
            Year = year.Value,
            Month = month.Value,
            SelectedEmployeeId = employeeId,
            Employees = empList,
        };

        if (employeeId.HasValue)
        {
            var emp = employees.FirstOrDefault(e => e.EmployeeId == employeeId.Value);
            if (emp != null)
            {
                model.EmployeeName = emp.FullName;
                model.EmployeeCode = emp.EmployeeCode;
            }

            var attendances = await _attendanceService.GetByEmployeeAndMonthAsync(employeeId.Value, year.Value, month.Value);
            model.AttendanceByDate = attendances.ToDictionary(a => a.Date);

            // Calculate standard work days: Mon-Fri = 2 shifts, Sat = 1 shift
            var firstDay = new DateOnly(year.Value, month.Value, 1);
            var daysInMonth = DateTime.DaysInMonth(year.Value, month.Value);
            decimal standardDays = 0;
            for (int i = 0; i < daysInMonth; i++)
            {
                var d = firstDay.AddDays(i);
                if (d.DayOfWeek == DayOfWeek.Saturday) standardDays += 1;
                else if (d.DayOfWeek != DayOfWeek.Sunday) standardDays += 2;
            }
            model.StandardWorkDays = standardDays;

            // Calculate summary with shift-based work points
            decimal totalDeductionMinutes = 0;
            foreach (var att in attendances)
            {
                if (att.WorkingHours.HasValue)
                {
                    bool isSat = att.Date.DayOfWeek == DayOfWeek.Saturday;
                    var wh = att.WorkingHours.Value;
                    if (isSat)
                        model.TotalWorkDays += wh >= 3 ? 1m : 0m;
                    else
                        model.TotalWorkDays += wh >= 8 ? 2m : (wh >= 6 ? 1.5m : (wh >= 3.5m ? 1m : 0m));
                }
                if (att.IsLate)
                {
                    model.LateDays++;
                    if (att.CheckIn.HasValue)
                    {
                        var lateMin = (att.CheckIn.Value.ToTimeSpan() - new TimeSpan(8, 30, 0)).TotalMinutes;
                        if (lateMin > 0) totalDeductionMinutes += (decimal)lateMin;
                    }
                }
                if (att.OvertimeHours.HasValue) model.TotalOvertimeHours += att.OvertimeHours.Value;
            }
            model.DeductionPoints = Math.Round(totalDeductionMinutes / 60m, 2);

            // Count absent weekdays (Mon-Sat without attendance, up to today)
            var todayDate = DateOnly.FromDateTime(DateTime.Now);
            for (int i = 0; i < daysInMonth; i++)
            {
                var d = firstDay.AddDays(i);
                if (d.DayOfWeek != DayOfWeek.Sunday && d <= todayDate && !model.AttendanceByDate.ContainsKey(d))
                    model.AbsentDays++;
            }
        }

        ViewBag.IsAdminOrManager = isAdminOrManager;
        return View(model);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> LateReport(int? year, int? month)
    {
        year ??= DateTime.UtcNow.Year;
        month ??= DateTime.UtcNow.Month;

        var lateRecords = await _attendanceService.GetLateCheckInsAsync(year.Value, month.Value);

        ViewBag.Year = year;
        ViewBag.Month = month;
        return View(lateRecords);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
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
