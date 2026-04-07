using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Models.ViewModels;
using managerCMN.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace managerCMN.Controllers;

[Authorize(Policy = "Authenticated")]
public class AttendanceController : Controller
{
    private const string FullAttendanceTableName = "FullAttendanceEmployees";

    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    private readonly IRequestService _requestService;
    private readonly IHolidayService _holidayService;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public AttendanceController(
        IAttendanceService attendanceService,
        IEmployeeService employeeService,
        IRequestService requestService,
        IHolidayService holidayService,
        ApplicationDbContext db,
        IWebHostEnvironment env)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _requestService = requestService;
        _holidayService = holidayService;
        _db = db;
        _env = env;
    }

    private static bool IsMissingFullAttendanceTable(SqlException ex)
        => ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase)
        && ex.Message.Contains(FullAttendanceTableName, StringComparison.OrdinalIgnoreCase);

    private int? GetCurrentEmployeeId()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        return int.TryParse(employeeIdClaim, out var id) ? id : null;
    }

    private async Task<bool> CanAccessAttendanceEmployeeAsync(int targetEmployeeId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            return true;

        var currentEmployeeId = GetCurrentEmployeeId();
        if (!currentEmployeeId.HasValue)
            return false;

        if (currentEmployeeId.Value == targetEmployeeId)
            return true;

        var employees = (await _employeeService.GetAllAsync()).ToList();
        var currentEmployee = employees.FirstOrDefault(employee => employee.EmployeeId == currentEmployeeId.Value);
        if (currentEmployee?.JobTitleId != 2 || !currentEmployee.DepartmentId.HasValue)
            return false;

        return employees.Any(employee =>
            employee.EmployeeId == targetEmployeeId
            && employee.DepartmentId == currentEmployee.DepartmentId);
    }

    private async Task<HashSet<int>> GetFullAttendanceEmployeeIdsAsync()
    {
        try
        {
            return await _db.FullAttendanceEmployees
                .Select(f => f.EmployeeId)
                .ToHashSetAsync();
        }
        catch (SqlException ex) when (IsMissingFullAttendanceTable(ex))
        {
            return [];
        }
    }

    public async Task<IActionResult> Index(int? year, int? month, int? employeeId)
    {
        var currentAttendancePeriod = AttendanceCalendarViewModel.GetDisplayPeriod(
            DateOnly.FromDateTime(DateTimeHelper.VietnamNow));
        year ??= currentAttendancePeriod.year;
        month ??= currentAttendancePeriod.month;

        bool isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Manager");
        int? myEmployeeId = null;
        Employee? currentEmployee = null;
        var empIdClaim = User.FindFirst("EmployeeId");
        if (empIdClaim != null && int.TryParse(empIdClaim.Value, out var eid))
        {
            myEmployeeId = eid;
            currentEmployee = (await _employeeService.GetAllAsync()).FirstOrDefault(e => e.EmployeeId == eid);
        }

        // Check if user is a department manager (JobTitleId = 2: Trưởng phòng)
        bool isDepartmentManager = currentEmployee?.JobTitleId == 2;
        int? myDepartmentId = currentEmployee?.DepartmentId;

        var employees = await _employeeService.GetAllAsync();
        List<EmployeeSelectItem> empList;

        if (isAdminOrManager)
        {
            // Admin/Manager: Show all employees
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
        else if (isDepartmentManager && myDepartmentId.HasValue)
        {
            // Department Manager: Show employees in their department
            empList = employees
                .Where(e => e.DepartmentId == myDepartmentId.Value)
                .OrderBy(e => e.FullName)
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
            // Regular employee: Show only self
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

        // Period: 26th prev month → 25th this month
        var (periodStart, periodEnd) = AttendanceCalendarViewModel.GetPeriodDates(year.Value, month.Value);

        // Load holidays for the period
        var holidays = (await _holidayService.GetByDateRangeAsync(periodStart, periodEnd))
            .Select(h => h.Date)
            .ToHashSet();
        ViewBag.Holidays = holidays;
        ViewBag.HolidayNames = (await _holidayService.GetByDateRangeAsync(periodStart, periodEnd))
            .ToDictionary(h => h.Date, h => h.Name);

        var model = new AttendanceCalendarViewModel
        {
            Year = year.Value,
            Month = month.Value,
            SelectedEmployeeId = employeeId,
            Employees = empList,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
        };

        var fullAttendanceEmployeeIds = await GetFullAttendanceEmployeeIdsAsync();
        var isFullAttendanceEmployee = employeeId.HasValue && fullAttendanceEmployeeIds.Contains(employeeId.Value);
        ViewBag.IsFullAttendanceEmployee = isFullAttendanceEmployee;

        if (employeeId.HasValue)
        {
            var emp = employees.FirstOrDefault(e => e.EmployeeId == employeeId.Value);
            var attendancePolicy = AttendancePolicyHelper.Resolve(emp?.JobTitleId);
            if (emp != null)
            {
                model.EmployeeName = emp.FullName;
                model.EmployeeCode = emp.EmployeeCode;
                model.SelectedEmployeeJobTitleId = emp.JobTitleId;
            }

            // Get attendance for the period (26 prev → 25 current)
            var attendances = await _attendanceService.GetByEmployeeAndDateRangeAsync(
                employeeId.Value, periodStart, periodEnd);
            model.AttendanceByDate = attendances.ToDictionary(a => a.Date);

            // Get all active requests for this employee overlapping the period
            var allRequests = await _requestService.GetByEmployeeAsync(employeeId.Value);
            var activeRequests = allRequests
                .Where(r => r.Status != RequestStatus.Rejected
                         && r.Status != RequestStatus.Cancelled)
                .ToList();

            // Build date → request info mapping
            model.RequestsByDate = AttendanceCalendarViewModel.BuildRequestCalendar(
                activeRequests,
                periodStart,
                periodEnd);

            // Calculate standard work days for the period
            decimal standardDays = 0;
            for (var d = periodStart; d <= periodEnd; d = d.AddDays(1))
            {
                if (AttendanceCalendarViewModel.IsWorkingDay(d))
                    standardDays += 1m;
            }
            model.StandardWorkDays = standardDays;

            // Shift constants
            var morningStart = AttendanceCalendarViewModel.MorningStart;
            var morningEnd = AttendanceCalendarViewModel.MorningEnd;
            var afternoonStart = AttendanceCalendarViewModel.AfternoonStart;
            var afternoonEnd = AttendanceCalendarViewModel.AfternoonEnd;

            // Calculate summary with 2-shift logic
            decimal totalDeductionMinutes = 0;
            decimal requestWorkDays = 0;
            var todayDate = DateOnly.FromDateTime(DateTimeHelper.VietnamNow);
            var useLegacyCoverage = false;

            foreach (var date in Enumerable.Range(0, periodEnd.DayNumber - periodStart.DayNumber + 1)
                         .Select(offset => periodStart.AddDays(offset)))
            {
                if (!AttendanceCalendarViewModel.IsWorkingDay(date, holidays))
                    continue;

                var attendance = model.AttendanceByDate.GetValueOrDefault(date);
                var approvedReqs = model.RequestsByDate.GetValueOrDefault(date)?
                    .Where(r => r.IsApproved)
                    .ToList() ?? [];

                var rawCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(date, attendance, policy: attendancePolicy);
                model.TotalWorkDays += rawCoverage.WorkPoints;

                var lateDuration = AttendanceCalendarViewModel.GetLateDuration(date, attendance, approvedReqs, attendancePolicy);
                if (lateDuration > TimeSpan.Zero)
                {
                    model.LateDays++;
                    totalDeductionMinutes += (decimal)lateDuration.TotalMinutes;
                }
            }

            foreach (var att in Enumerable.Empty<managerCMN.Models.Entities.Attendance>())
            {
                // Check if there's an approved request for this date
                var rawCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(att.Date, att, policy: attendancePolicy);
                model.TotalWorkDays += rawCoverage.WorkPoints;

                // If checkin is after 10:00 AM and no approved request, skip counting this attendance
                if (useLegacyCoverage)
                    continue;

                if (useLegacyCoverage && att.CheckIn.HasValue && att.CheckOut.HasValue)
                {
                    // Check if covered both shifts: morning 8:30-12:00, afternoon 13:30-17:30.
                    // Working Saturdays use 15:00 as the minimum checkout for afternoon session.
                    var checkIn = att.CheckIn.Value;
                    var checkOut = att.CheckOut.Value;
                    var minAfternoonCheckOut = AttendanceCalendarViewModel.GetMinAfternoonCheckOut(att.Date);
                    bool hasMorning = checkIn <= morningEnd && checkOut >= morningStart;
                    bool hasAfternoon = checkIn <= afternoonEnd && checkOut >= minAfternoonCheckOut;

                    if (hasMorning && hasAfternoon)
                        model.TotalWorkDays += 1m;
                    else if (hasMorning || hasAfternoon)
                        model.TotalWorkDays += 0.5m;
                }
                else if (useLegacyCoverage && att.CheckIn.HasValue)
                {
                    // Only check-in, no check-out → count as half day (morning only)
                    model.TotalWorkDays += 0.5m;
                }

                if (att.IsLate)
                {
                    model.LateDays++;
                    if (att.CheckIn.HasValue)
                    {
                        var lateMin = (att.CheckIn.Value.ToTimeSpan() - morningStart.ToTimeSpan()).TotalMinutes;
                        if (lateMin > 0) totalDeductionMinutes += (decimal)lateMin;
                    }
                }
            }

            // Add công from FULLY APPROVED requests with CountsAsWork
            foreach (var kvp in model.RequestsByDate)
            {
                var date = kvp.Key;
                if (!AttendanceCalendarViewModel.IsWorkingDay(date)) continue;

                // Only count approved requests for công calculation
                var approvedReqs = kvp.Value.Where(r => r.IsApproved).ToList();
                if (!approvedReqs.Any()) continue;

                // Calculate request công for this date (don't double count with attendance)
                var attendance = model.AttendanceByDate.GetValueOrDefault(date);
                var rawCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(date, attendance, policy: attendancePolicy);
                var correctedCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(date, attendance, approvedReqs, attendancePolicy);
                var requestCoverage = AttendanceCalendarViewModel.GetApprovedRequestShiftCoverage(approvedReqs);
                decimal attCong = correctedCoverage.WorkPoints;

                // Check which shifts are covered by approved requests
                bool morningCoveredByRequest = requestCoverage.Morning;
                bool afternoonCoveredByRequest = requestCoverage.Afternoon;
                foreach (var reqInfo in approvedReqs)
                {
                    if (!reqInfo.CountsAsWork || reqInfo.RequestType == RequestType.CheckInOut) continue;
                    if (reqInfo.IsHalfDayMorning == null) // full day
                    {
                        morningCoveredByRequest = true;
                        afternoonCoveredByRequest = true;
                    }
                    else if (reqInfo.IsHalfDayMorning == true) // morning only
                    {
                        morningCoveredByRequest = true;
                    }
                    else // afternoon only
                    {
                        afternoonCoveredByRequest = true;
                    }
                }

                decimal reqCong = 0;
                if (morningCoveredByRequest && afternoonCoveredByRequest) reqCong = 1m;
                else if (morningCoveredByRequest || afternoonCoveredByRequest) reqCong = 0.5m;

                // Total for this day = min(attCong + reqCong, 1) — don't exceed 1 công
                decimal dayCong = Math.Min(attCong + reqCong, 1m);
                decimal extraFromRequest = dayCong - rawCoverage.WorkPoints;
                if (extraFromRequest > 0)
                    requestWorkDays += extraFromRequest;
            }
            model.RequestWorkDays = requestWorkDays;
            model.DeductionPoints = Math.Round(totalDeductionMinutes / 60m, 2);

            // Count absent work days (up to today): no attendance AND no approved CountsAsWork request
            for (var d = periodStart; d <= periodEnd && d <= todayDate; d = d.AddDays(1))
            {
                if (!AttendanceCalendarViewModel.IsWorkingDay(d)) continue;

                var attendance = model.AttendanceByDate.GetValueOrDefault(d);
                var approvedReqs = model.RequestsByDate.GetValueOrDefault(d)?
                    .Where(r => r.IsApproved)
                    .ToList() ?? [];
                var correctedCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(d, attendance, approvedReqs, attendancePolicy);
                var requestCoverage = AttendanceCalendarViewModel.GetApprovedRequestShiftCoverage(approvedReqs);
                var finalPoints = AttendanceCalendarViewModel.GetWorkPoints(
                    correctedCoverage.HasMorning || requestCoverage.Morning,
                    correctedCoverage.HasAfternoon || requestCoverage.Afternoon);
                var hasApprovedNonWorkRequest = approvedReqs.Any(r => !r.CountsAsWork);

                if (finalPoints == 0 && !hasApprovedNonWorkRequest)
                    model.AbsentDays++;

                if (useLegacyCoverage)
                {
                    // Check if there's any approved request at all (non-counting-as-work like unpaid leave)
                    bool hasAnyApprovedReq = model.RequestsByDate.ContainsKey(d)
                        && model.RequestsByDate[d].Any(r => r.IsApproved);
                    if (!hasAnyApprovedReq)
                        model.AbsentDays++;
                    // If there's a non-CountsAsWork approved request → not counted as absent, just 0 công
                }
            }

            // Full attendance employees are auto-marked as full workday on every working day in period (up to today).
            if (isFullAttendanceEmployee)
            {
                decimal autoWorkDays = 0;
                for (var d = periodStart; d <= periodEnd && d <= todayDate; d = d.AddDays(1))
                {
                    if (AttendanceCalendarViewModel.IsWorkingDay(d, holidays))
                    {
                        autoWorkDays += 1m;
                    }
                }

                model.TotalWorkDays = autoWorkDays;
                model.RequestWorkDays = 0;
                model.LateDays = 0;
                model.AbsentDays = 0;
                model.DeductionPoints = 0;
            }
        }

        ViewBag.IsAdminOrManager = isAdminOrManager;
        ViewBag.IsDepartmentManager = isDepartmentManager;
        return View(model);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> LateReport(int? year, int? month)
    {
        var currentAttendancePeriod = AttendanceCalendarViewModel.GetDisplayPeriod(
            DateOnly.FromDateTime(DateTimeHelper.VietnamNow));
        year ??= currentAttendancePeriod.year;
        month ??= currentAttendancePeriod.month;

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
        // Validate using FileUploadHelper
        var validationResult = FileUploadHelper.ValidateFile(
            model.ExcelFile,
            FileUploadHelper.AllowedExcelExtensions,
            true);

        if (validationResult != ValidationResult.Success)
        {
            ModelState.AddModelError("", validationResult.ErrorMessage ?? "File không hợp lệ.");
            return View(model);
        }

        using var stream = model.ExcelFile!.OpenReadStream();
        await _attendanceService.ImportFromExcelAsync(stream);

        TempData["Success"] = "Import chấm công thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> ExportToExcel(int? year, int? month)
    {
        var currentAttendancePeriod = AttendanceCalendarViewModel.GetDisplayPeriod(
            DateOnly.FromDateTime(DateTimeHelper.VietnamNow));
        year ??= currentAttendancePeriod.year;
        month ??= currentAttendancePeriod.month;

        var excelBytes = await _attendanceService.ExportToExcelAsync(year.Value, month.Value);
        var fileName = $"BangChamCong_{year}_{month:D2}.xlsx";
        
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Summary(int? year, int? month)
    {
        var currentAttendancePeriod = AttendanceCalendarViewModel.GetDisplayPeriod(
            DateOnly.FromDateTime(DateTimeHelper.VietnamNow));
        year ??= currentAttendancePeriod.year;
        month ??= currentAttendancePeriod.month;

        var employees = await _employeeService.GetAllAsync();
        var (periodStart, periodEnd) = AttendanceCalendarViewModel.GetPeriodDates(year.Value, month.Value);
        
        var summaryList = new List<AttendanceSummaryViewModel>();
        
        foreach (var emp in employees.OrderBy(e => e.Department?.DepartmentName).ThenBy(e => e.FullName))
        {
            var attendancePolicy = AttendancePolicyHelper.Resolve(emp.JobTitleId);
            var attendances = await _attendanceService.GetByEmployeeAndDateRangeAsync(emp.EmployeeId, periodStart, periodEnd);
            var requests = await _requestService.GetByEmployeeAsync(emp.EmployeeId);
            
            var activeRequests = requests
                .Where(r => r.Status != RequestStatus.Rejected && r.Status != RequestStatus.Cancelled)
                .ToList();
            
            // Calculate statistics
            decimal totalCong = 0;
            decimal donCoPhep = 0;
            decimal donKhongPhep = 0;
            int totalLateMinutes = 0;
            var requestsByDate = AttendanceCalendarViewModel.BuildRequestCalendar(activeRequests, periodStart, periodEnd);
            var attendanceByDate = attendances.ToDictionary(a => a.Date);

            // Get approved checkin requests for this employee in this period
            var approvedCheckinRequests = activeRequests
                .Where(r => r.RequestType == RequestType.CheckInOut
                         && r.Status == RequestStatus.FullyApproved
                         && r.StartTime.Date >= periodStart.ToDateTime(TimeOnly.MinValue)
                         && r.EndTime.Date <= periodEnd.ToDateTime(TimeOnly.MinValue))
                .Select(r => DateOnly.FromDateTime(r.StartTime))
                .ToHashSet();

            // Count attendance công
            var morningStart = AttendanceCalendarViewModel.MorningStart;
            var morningEnd = AttendanceCalendarViewModel.MorningEnd;
            var afternoonStart = AttendanceCalendarViewModel.AfternoonStart;
            var afternoonEnd = AttendanceCalendarViewModel.AfternoonEnd;

            foreach (var date in Enumerable.Range(0, periodEnd.DayNumber - periodStart.DayNumber + 1)
                         .Select(offset => periodStart.AddDays(offset)))
            {
                if (!AttendanceCalendarViewModel.IsWorkingDay(date))
                    continue;

                var attendance = attendanceByDate.GetValueOrDefault(date);
                var approvedReqs = requestsByDate.GetValueOrDefault(date)?
                    .Where(r => r.IsApproved)
                    .ToList() ?? [];
                var correctedCoverage = AttendanceCalendarViewModel.EvaluateAttendanceCoverage(date, attendance, approvedReqs, attendancePolicy);
                var requestCoverage = AttendanceCalendarViewModel.GetApprovedRequestShiftCoverage(approvedReqs);
                var finalPoints = AttendanceCalendarViewModel.GetWorkPoints(
                    correctedCoverage.HasMorning || requestCoverage.Morning,
                    correctedCoverage.HasAfternoon || requestCoverage.Afternoon);

                totalCong += finalPoints;

                var lateDuration = AttendanceCalendarViewModel.GetLateDuration(date, attendance, approvedReqs, attendancePolicy);
                if (lateDuration > TimeSpan.Zero)
                {
                    totalLateMinutes += (int)lateDuration.TotalMinutes;
                }
            }

            foreach (var att in Enumerable.Empty<managerCMN.Models.Entities.Attendance>())
            {
                if (att.CheckIn.HasValue && att.CheckOut.HasValue)
                {
                    var minAfternoonCheckOut = AttendanceCalendarViewModel.GetMinAfternoonCheckOut(att.Date);
                    bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
                    bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= minAfternoonCheckOut;

                    if (hasMorning && hasAfternoon)
                        totalCong += 1m;
                    else if (hasMorning || hasAfternoon)
                        totalCong += 0.5m;
                }
                else if (att.CheckIn.HasValue)
                {
                    totalCong += 0.5m;
                }

                // Calculate late minutes only if no approved checkin request for this date
                if (att.IsLate && !approvedCheckinRequests.Contains(att.Date) && att.CheckIn.HasValue)
                {
                    // Calculate late minutes based on check-in time
                    int lateMinutes = 0;

                    // Check morning session (8:30 AM)
                    if (att.CheckIn.Value > morningStart)
                    {
                        // If checked in after morning start, calculate late minutes for morning
                        var morningLateMinutes = (int)(att.CheckIn.Value - morningStart).TotalMinutes;
                        lateMinutes = Math.Max(0, morningLateMinutes);
                    }

                    // If attending afternoon session and late
                    if (att.CheckIn.Value > afternoonStart && att.CheckOut.HasValue && att.CheckOut.Value >= afternoonStart)
                    {
                        // Check if this is afternoon-only attendance
                        bool isMorningOnly = att.CheckOut.Value < afternoonStart;
                        if (!isMorningOnly)
                        {
                            var afternoonLateMinutes = (int)(att.CheckIn.Value - afternoonStart).TotalMinutes;
                            // Use afternoon late minutes if it's worse than morning
                            lateMinutes = Math.Max(lateMinutes, Math.Max(0, afternoonLateMinutes));
                        }
                    }

                    totalLateMinutes += lateMinutes;
                }
            }

            // Count approved requests
            foreach (var req in activeRequests)
            {
                if (req.Status == RequestStatus.FullyApproved)
                {
                    var reqStart = DateOnly.FromDateTime(req.StartTime);
                    var reqEnd = DateOnly.FromDateTime(req.EndTime);

                    if (req.RequestType == RequestType.Leave)
                    {
                        if (req.CountsAsWork)
                            donCoPhep += req.TotalDays;
                        else
                            donKhongPhep += req.TotalDays;
                    }
                }
            }

            summaryList.Add(new AttendanceSummaryViewModel
            {
                EmployeeId = emp.EmployeeId,
                EmployeeCode = emp.EmployeeCode ?? "",
                FullName = emp.FullName,
                DepartmentName = emp.Department?.DepartmentName ?? "",
                TotalCong = totalCong,
                DonCoPhep = donCoPhep,
                DonKhongPhep = donKhongPhep,
                LateMinutes = totalLateMinutes  // Thay đổi từ LateDays thành LateMinutes
            });
        }
        
        ViewBag.Year = year;
        ViewBag.Month = month;
        return View(summaryList);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLateMinutes(int? year, int? month)
    {
        try
        {
            var currentAttendancePeriod = AttendanceCalendarViewModel.GetDisplayPeriod(
                DateOnly.FromDateTime(DateTimeHelper.VietnamNow));
            year ??= currentAttendancePeriod.year;
            month ??= currentAttendancePeriod.month;
            var (periodStart, periodEnd) = AttendanceCalendarViewModel.GetPeriodDates(year.Value, month.Value);

            var updatedCount = await _attendanceService.UpdateExistingLateMinutesAsync(periodStart, periodEnd);

            if (updatedCount > 0)
            {
                TempData["Success"] = $"Đã cập nhật {updatedCount} bản ghi chấm công với thông tin phút đi muộn.";
            }
            else
            {
                TempData["Info"] = "Không có bản ghi nào cần cập nhật.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi cập nhật: {ex.Message}";
        }

        return RedirectToAction(nameof(Summary), new { year, month });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecalculateAttendanceTimes(int? year, int? month)
    {
        try
        {
            var currentAttendancePeriod = AttendanceCalendarViewModel.GetDisplayPeriod(
                DateOnly.FromDateTime(DateTimeHelper.VietnamNow));
            year ??= currentAttendancePeriod.year;
            month ??= currentAttendancePeriod.month;
            var (periodStart, periodEnd) = AttendanceCalendarViewModel.GetPeriodDates(year.Value, month.Value);

            var updatedCount = await _attendanceService.RecalculateAllAttendanceTimesAsync(periodStart, periodEnd);

            if (updatedCount > 0)
            {
                TempData["Success"] = $"Đã đồng bộ lại {updatedCount} bản ghi chấm công (CheckIn/CheckOut) từ PunchRecords.";
            }
            else
            {
                TempData["Info"] = "Tất cả giờ chấm công đã đúng, không có bản ghi nào cần cập nhật.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi đồng bộ: {ex.Message}";
        }

        return RedirectToAction(nameof(Summary), new { year, month });
    }

    /// <summary>
    /// API endpoint to get all punch records for a specific employee and date (for modal display)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPunchRecords(int employeeId, string date)
    {
        if (!await CanAccessAttendanceEmployeeAsync(employeeId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                success = false,
                error = "Bạn không có quyền xem dữ liệu chấm công này."
            });
        }
        Console.WriteLine($"🔍 GetPunchRecords called - EmployeeId: {employeeId}, Date: '{date}'");

        // Clean up date string (remove quotes if any)
        var cleanedDate = date?.Trim('"', '\'', ' ') ?? "";
        Console.WriteLine($"🧹 Cleaned date: '{cleanedDate}'");

        if (string.IsNullOrEmpty(cleanedDate))
        {
            Console.WriteLine($"❌ Empty date string");
            return Json(new { success = false, error = "Date is required" });
        }

        if (!DateOnly.TryParse(cleanedDate, out var parsedDate))
        {
            Console.WriteLine($"❌ Invalid date format: '{cleanedDate}'");
            return Json(new { success = false, error = $"Invalid date format: {cleanedDate}" });
        }

        Console.WriteLine($"✅ Parsed date: {parsedDate:yyyy-MM-dd}");

        try
        {
            var punchRecords = await _attendanceService.GetPunchRecordsByDateAsync(employeeId, parsedDate);
            var recordsList = punchRecords.ToList();
            Console.WriteLine($"📊 Found {recordsList.Count} punch records for Employee {employeeId} on {parsedDate:yyyy-MM-dd}");

            var result = recordsList.Select(pr => new
            {
                punchRecordId = pr.PunchRecordId,
                punchTime = pr.PunchTime.ToString("HH:mm"),
                sequenceNumber = pr.SequenceNumber,
                sourceTimestamp = pr.SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                deviceId = pr.DeviceId
            }).ToList();

            Console.WriteLine($"✅ Returning {result.Count} records");
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception: {ex.Message}");
            return Json(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Debug endpoint to test punch records directly
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TestPunchRecords()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        // Hardcoded test for Employee 55 on 2026-03-25
        var testEmployeeId = 55;
        var testDate = new DateOnly(2026, 3, 25);

        Console.WriteLine($"🧪 TEST: Getting punch records for Employee {testEmployeeId} on {testDate:yyyy-MM-dd}");

        var punchRecords = await _attendanceService.GetPunchRecordsByDateAsync(testEmployeeId, testDate);
        var recordsList = punchRecords.ToList();

        Console.WriteLine($"🧪 TEST: Found {recordsList.Count} records");

        var result = recordsList.Select(pr => new
        {
            punchRecordId = pr.PunchRecordId,
            employeeId = pr.EmployeeId,
            date = pr.Date.ToString("yyyy-MM-dd"),
            punchTime = pr.PunchTime.ToString("HH:mm:ss"),
            sequenceNumber = pr.SequenceNumber
        }).ToList();

        return Json(new {
            testEmployeeId,
            testDate = testDate.ToString("yyyy-MM-dd"),
            recordCount = result.Count,
            records = result
        });
    }
}
