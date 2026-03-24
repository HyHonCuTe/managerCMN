using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    private readonly IRequestService _requestService;
    private readonly IHolidayService _holidayService;

    public AttendanceController(
        IAttendanceService attendanceService,
        IEmployeeService employeeService,
        IRequestService requestService,
        IHolidayService holidayService)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _requestService = requestService;
        _holidayService = holidayService;
    }

    public async Task<IActionResult> Index(int? year, int? month, int? employeeId)
    {
        year ??= DateTimeHelper.VietnamNow.Year;
        month ??= DateTimeHelper.VietnamNow.Month;

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

        if (employeeId.HasValue)
        {
            var emp = employees.FirstOrDefault(e => e.EmployeeId == employeeId.Value);
            if (emp != null)
            {
                model.EmployeeName = emp.FullName;
                model.EmployeeCode = emp.EmployeeCode;
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
            foreach (var req in activeRequests)
            {
                var reqStart = DateOnly.FromDateTime(req.StartTime);
                var reqEnd = DateOnly.FromDateTime(req.EndTime);
                var totalReqDays = reqEnd.DayNumber - reqStart.DayNumber + 1;
                var isApproved = req.Status == RequestStatus.FullyApproved;

                for (var d = reqStart; d <= reqEnd; d = d.AddDays(1))
                {
                    if (d >= periodStart && d <= periodEnd)
                    {
                        if (!model.RequestsByDate.ContainsKey(d))
                            model.RequestsByDate[d] = new List<RequestDayInfo>();

                        // Determine half-day info for this specific date
                        bool? isHalfDayMorning = null; // null = full day
                        if (d == reqStart && req.IsHalfDayStart)
                            isHalfDayMorning = req.IsHalfDayStartMorning; // true=morning, false=afternoon
                        else if (d == reqEnd && req.IsHalfDayEnd)
                            isHalfDayMorning = req.IsHalfDayEndMorning; // true=morning, false=afternoon

                        model.RequestsByDate[d].Add(new RequestDayInfo
                        {
                            RequestId = req.RequestId,
                            RequestType = req.RequestType,
                            CountsAsWork = req.CountsAsWork,
                            IsHalfDayMorning = isHalfDayMorning,
                            IsApproved = isApproved
                        });
                    }
                }
            }

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

            foreach (var att in attendances)
            {
                if (att.CheckIn.HasValue && att.CheckOut.HasValue)
                {
                    // Check if covered both shifts: morning 8:30-12:00, afternoon 13:30-17:30
                    var checkIn = att.CheckIn.Value;
                    var checkOut = att.CheckOut.Value;
                    bool hasMorning = checkIn <= morningEnd && checkOut >= morningStart;
                    bool hasAfternoon = checkIn <= afternoonEnd && checkOut >= afternoonStart;

                    if (hasMorning && hasAfternoon)
                        model.TotalWorkDays += 1m;
                    else if (hasMorning || hasAfternoon)
                        model.TotalWorkDays += 0.5m;
                }
                else if (att.CheckIn.HasValue)
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
                bool hasAttendance = model.AttendanceByDate.ContainsKey(date);
                decimal attCong = 0;
                if (hasAttendance)
                {
                    var att = model.AttendanceByDate[date];
                    if (att.CheckIn.HasValue && att.CheckOut.HasValue)
                    {
                        bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
                        bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= afternoonStart;
                        if (hasMorning && hasAfternoon) attCong = 1m;
                        else if (hasMorning || hasAfternoon) attCong = 0.5m;
                    }
                    else if (att.CheckIn.HasValue)
                    {
                        attCong = 0.5m;
                    }
                }

                // Check which shifts are covered by approved requests
                bool morningCoveredByRequest = false;
                bool afternoonCoveredByRequest = false;
                foreach (var reqInfo in approvedReqs)
                {
                    if (!reqInfo.CountsAsWork) continue;
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
                decimal extraFromRequest = dayCong - attCong;
                if (extraFromRequest > 0)
                    requestWorkDays += extraFromRequest;
            }
            model.RequestWorkDays = requestWorkDays;
            model.DeductionPoints = Math.Round(totalDeductionMinutes / 60m, 2);

            // Count absent work days (up to today): no attendance AND no approved CountsAsWork request
            for (var d = periodStart; d <= periodEnd && d <= todayDate; d = d.AddDays(1))
            {
                if (!AttendanceCalendarViewModel.IsWorkingDay(d)) continue;

                bool hasAtt = model.AttendanceByDate.ContainsKey(d);
                bool hasApprovedCountsAsWorkReq = model.RequestsByDate.ContainsKey(d)
                    && model.RequestsByDate[d].Any(r => r.IsApproved && r.CountsAsWork);

                if (!hasAtt && !hasApprovedCountsAsWorkReq)
                {
                    // Check if there's any approved request at all (non-counting-as-work like unpaid leave)
                    bool hasAnyApprovedReq = model.RequestsByDate.ContainsKey(d)
                        && model.RequestsByDate[d].Any(r => r.IsApproved);
                    if (!hasAnyApprovedReq)
                        model.AbsentDays++;
                    // If there's a non-CountsAsWork approved request → not counted as absent, just 0 công
                }
            }
        }

        ViewBag.IsAdminOrManager = isAdminOrManager;
        ViewBag.IsDepartmentManager = isDepartmentManager;
        return View(model);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> LateReport(int? year, int? month)
    {
        year ??= DateTimeHelper.VietnamNow.Year;
        month ??= DateTimeHelper.VietnamNow.Month;

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

        using var stream = model.ExcelFile.OpenReadStream();
        await _attendanceService.ImportFromExcelAsync(stream);

        TempData["Success"] = "Import chấm công thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> ExportToExcel(int? year, int? month)
    {
        year ??= DateTimeHelper.VietnamNow.Year;
        month ??= DateTimeHelper.VietnamNow.Month;

        var excelBytes = await _attendanceService.ExportToExcelAsync(year.Value, month.Value);
        var fileName = $"BangChamCong_{year}_{month:D2}.xlsx";
        
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [Authorize(Policy = "ManagerOrAdmin")]
    public async Task<IActionResult> Summary(int? year, int? month)
    {
        year ??= DateTimeHelper.VietnamNow.Year;
        month ??= DateTimeHelper.VietnamNow.Month;

        var employees = await _employeeService.GetAllAsync();
        var (periodStart, periodEnd) = AttendanceCalendarViewModel.GetPeriodDates(year.Value, month.Value);
        
        var summaryList = new List<AttendanceSummaryViewModel>();
        
        foreach (var emp in employees.OrderBy(e => e.Department?.DepartmentName).ThenBy(e => e.FullName))
        {
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

            foreach (var att in attendances)
            {
                if (att.CheckIn.HasValue && att.CheckOut.HasValue)
                {
                    bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
                    bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= afternoonStart;

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
    public async Task<IActionResult> UpdateLateMinutes()
    {
        try
        {
            var updatedCount = await _attendanceService.UpdateExistingLateMinutesAsync();

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

        return RedirectToAction(nameof(Summary));
    }
}
