using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using managerCMN.Models.Entities;
using managerCMN.Models.ViewModels;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly TimeOnly LateThreshold = new(8, 30); // 8:30 AM

    public AttendanceService(IUnitOfWork unitOfWork, ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public async Task<IEnumerable<Attendance>> GetByEmployeeAndMonthAsync(int employeeId, int year, int month)
        => await _unitOfWork.Attendances.GetByEmployeeAndMonthAsync(employeeId, year, month);

    public async Task<IEnumerable<Attendance>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate)
        => await _unitOfWork.Attendances.GetByEmployeeAndDateRangeAsync(employeeId, startDate, endDate);

    public async Task<IEnumerable<Attendance>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
        => await _unitOfWork.Attendances.GetByDateRangeAsync(startDate, endDate);

    public async Task<IEnumerable<Attendance>> GetLateCheckInsAsync(int year, int month)
        => await _unitOfWork.Attendances.GetLateCheckInsAsync(year, month, LateThreshold);

    public async Task<Attendance?> GetByIdAsync(int id)
        => await _unitOfWork.Attendances.GetByIdAsync(id);

    public async Task ImportFromExcelAsync(Stream excelStream)
    {
        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();
        var rows = worksheet.RowsUsed().Skip(1); // Skip header

        // Format: Column A (1) = UserID (AttendanceCode), Column B (2) = Time
        var punchRecords = new List<(string AttendanceCode, DateTime PunchTime)>();

        foreach (var row in rows)
        {
            var userIdStr = row.Cell(1).GetString().Trim();
            if (string.IsNullOrEmpty(userIdStr)) continue;

            DateTime punchTime;
            if (row.Cell(2).DataType == XLDataType.DateTime)
            {
                punchTime = row.Cell(2).GetDateTime();
            }
            else
            {
                var dtStr = row.Cell(2).GetString().Trim();
                if (!DateTime.TryParse(dtStr, out punchTime)) continue;
            }

            punchRecords.Add((userIdStr, punchTime));
        }

        await ProcessPunchRecordsAsync(punchRecords);

        if (punchRecords.Count > 0)
        {
            var dates = punchRecords.Select(r => r.PunchTime.Date).Distinct().OrderBy(d => d).ToList();
            var minDate = dates.First();
            var maxDate = dates.Last();

            await _logService.LogAsync(
                GetCurrentUserId(),
                "Import chấm công từ Excel",
                "Attendance",
                null,
                new { RecordCount = punchRecords.Count, DateRange = $"{minDate:dd/MM/yyyy} - {maxDate:dd/MM/yyyy}" },
                GetClientIP()
            );
        }
    }

    public async Task ProcessPunchRecordsAsync(IEnumerable<(string AttendanceCode, DateTime PunchTime)> punchRecords)
    {
        var grouped = punchRecords
            .GroupBy(r => (r.AttendanceCode, r.PunchTime.Date))
            .ToList();

        var empCache = new Dictionary<string, Employee?>();

        foreach (var group in grouped)
        {
            var attCode = group.Key.AttendanceCode;
            var date = DateOnly.FromDateTime(group.Key.Date);

            // Resolve employee
            if (!empCache.ContainsKey(attCode))
                empCache[attCode] = await _unitOfWork.Employees.GetByAttendanceCodeAsync(attCode);

            var employee = empCache[attCode];
            if (employee == null) continue;

            var punches = group.OrderBy(r => r.PunchTime).ToList();

            // ==== STEP 1: Save ALL punch records to PunchRecord table ====
            var maxSeq = await _unitOfWork.PunchRecords.GetMaxSequenceNumberAsync(employee.EmployeeId, date);
            int currentSeq = maxSeq;

            foreach (var punch in punches)
            {
                var punchTime = TimeOnly.FromDateTime(punch.PunchTime);

                // Check for duplicate (idempotency - avoid inserting same punch twice)
                var exists = await _unitOfWork.PunchRecords.ExistsAsync(employee.EmployeeId, date, punchTime);
                if (exists) continue;

                currentSeq++;
                var punchRecord = new PunchRecord
                {
                    EmployeeId = employee.EmployeeId,
                    Date = date,
                    PunchTime = punchTime,
                    SourceTimestamp = punch.PunchTime,
                    SequenceNumber = currentSeq,
                    DeviceId = null // Could be extracted from source if available
                };

                await _unitOfWork.PunchRecords.AddAsync(punchRecord);
            }

            // CRITICAL: Save punch records to database FIRST before calculating CheckIn/CheckOut
            // This ensures the query below includes the newly added punch records
            await _unitOfWork.SaveChangesAsync();

            // ==== STEP 2: Update or Insert Attendance (first/last logic) ====
            // IMPORTANT: Load ALL punch records from database (including newly added ones) to calculate correct CheckIn/CheckOut
            var allPunchRecords = await _unitOfWork.PunchRecords.GetByEmployeeAndDateAsync(employee.EmployeeId, date);
            var allPunches = allPunchRecords.OrderBy(pr => pr.PunchTime).ToList();

            if (!allPunches.Any())
                continue; // No punches, skip attendance update

            var checkIn = allPunches.First().PunchTime;
            TimeOnly? checkOut = null;

            // Only set checkOut if there are multiple punches AND the last punch is different from first
            if (allPunches.Count > 1)
            {
                var lastPunchTime = allPunches.Last().PunchTime;
                if (lastPunchTime != checkIn)
                {
                    checkOut = lastPunchTime;
                }
            }

            // Calculate working hours
            decimal? workingHours = null;
            if (checkOut.HasValue)
                workingHours = Math.Round((decimal)(checkOut.Value.ToTimeSpan() - checkIn.ToTimeSpan()).TotalHours, 2);

            // Calculate late minutes
            var isLate = checkIn > LateThreshold;
            var lateMinutes = isLate ? (int)(checkIn - LateThreshold).TotalMinutes : 0;

            // Check if attendance exists for this day
            var existingAttendance = await _unitOfWork.Attendances.Query()
                .FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == date);

            if (existingAttendance != null)
            {
                // UPDATE existing record
                existingAttendance.CheckIn = checkIn;
                existingAttendance.CheckOut = checkOut;
                existingAttendance.WorkingHours = workingHours;
                existingAttendance.IsLate = isLate;
                existingAttendance.LateMinutes = lateMinutes;
                _unitOfWork.Attendances.Update(existingAttendance);
            }
            else
            {
                // INSERT new record
                var attendance = new Attendance
                {
                    EmployeeId = employee.EmployeeId,
                    Date = date,
                    CheckIn = checkIn,
                    CheckOut = checkOut,
                    WorkingHours = workingHours,
                    OvertimeHours = 0,
                    IsLate = isLate,
                    LateMinutes = lateMinutes
                };

                await _unitOfWork.Attendances.AddAsync(attendance);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<PunchRecord>> GetPunchRecordsByDateAsync(int employeeId, DateOnly date)
    {
        return await _unitOfWork.PunchRecords.GetByEmployeeAndDateAsync(employeeId, date);
    }

    public async Task<byte[]> ExportToExcelAsync(int year, int month)
    {
        var (periodStart, periodEnd) = GetPeriodDates(year, month);

        // Get holidays for the period
        var holidays = (await _unitOfWork.Holidays.GetByDateRangeAsync(periodStart, periodEnd))
            .Select(h => h.Date)
            .ToHashSet();

        // Get all employees sorted by Employee Code
        var employees = (await _unitOfWork.Employees.GetAllAsync())
            .OrderBy(e => e.EmployeeCode)
            .ToList();

        // Get all attendance for the period
        var attendances = await GetByDateRangeAsync(periodStart, periodEnd);
        var attendanceDict = attendances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.Date));

        // Get all requests for all employees for the period
        var allRequests = new Dictionary<int, List<Models.Entities.Request>>();
        foreach (var emp in employees)
        {
            var empRequests = (await _unitOfWork.Requests.GetByEmployeeAsync(emp.EmployeeId))
                .Where(r => r.Status != Models.Enums.RequestStatus.Rejected &&
                           r.Status != Models.Enums.RequestStatus.Cancelled)
                .ToList();
            allRequests[emp.EmployeeId] = empRequests;
        }

        // Get list of employees with automatic full attendance
        var fullAttendanceEmployeeIds = (await _unitOfWork.FullAttendanceEmployees.FindAsync(e => true))
            .Select(f => f.EmployeeId)
            .ToHashSet();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bảng chấm công");

        // Shift constants
        var morningStart = new TimeOnly(8, 30);
        var morningEnd = new TimeOnly(12, 0);
        var afternoonStart = new TimeOnly(13, 30);
        var afternoonEnd = new TimeOnly(17, 30);
        // minAfternoonCheckOut is a class-level constant

        // Header row
        int col = 1;
        worksheet.Cell(1, col++).Value = "#";
        worksheet.Cell(1, col++).Value = "Mã NV";
        worksheet.Cell(1, col++).Value = "Họ tên";
        worksheet.Cell(1, col++).Value = "Phòng ban";

        // Date columns
        var dateColumns = new List<DateOnly>();
        for (var d = periodStart; d <= periodEnd; d = d.AddDays(1))
        {
            dateColumns.Add(d);
            worksheet.Cell(1, col++).Value = d.ToString("dd/MM");
        }

        // Summary columns
        worksheet.Cell(1, col++).Value = "Tổng công";
        worksheet.Cell(1, col++).Value = "Đơn P";
        worksheet.Cell(1, col++).Value = "Đơn K";
        worksheet.Cell(1, col++).Value = "Ngày lễ";

        // Style header
        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Data rows
        int rowNum = 2;
        foreach (var emp in employees)
        {
            col = 1;
            worksheet.Cell(rowNum, col++).Value = rowNum - 1;
            worksheet.Cell(rowNum, col++).Value = emp.EmployeeCode ?? "";
            worksheet.Cell(rowNum, col++).Value = emp.FullName;
            worksheet.Cell(rowNum, col++).Value = emp.Department?.DepartmentName ?? "";

            decimal totalCong = 0;
            decimal totalP = 0;
            decimal totalK = 0;
            decimal totalHolidays = 0;

            var empAttendances = attendanceDict.ContainsKey(emp.EmployeeId)
                ? attendanceDict[emp.EmployeeId]
                : new Dictionary<DateOnly, Models.Entities.Attendance>();

            var empRequests = allRequests[emp.EmployeeId];

            // Date cells
            foreach (var date in dateColumns)
            {
                var cellValue = GetCellValue(date,
                    empAttendances.GetValueOrDefault(date),
                    empRequests,
                    holidays,
                    morningStart, morningEnd, afternoonStart, afternoonEnd,
                    emp.EmployeeId, fullAttendanceEmployeeIds,
                    out var dayCong, out var dayP, out var dayK, out var dayHoliday);

                totalCong += dayCong;
                totalP += dayP;
                totalK += dayK;
                totalHolidays += dayHoliday;

                var cell = worksheet.Cell(rowNum, col);
                cell.Value = cellValue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Apply color coding
                if (cellValue == "1" || cellValue == "1/2")
                {
                    cell.Style.Fill.BackgroundColor = XLColor.LightGreen; // Green for attendance/counts-as-work
                }
                else if (cellValue == "P" || cellValue == "P/2")
                {
                    cell.Style.Fill.BackgroundColor = XLColor.Yellow; // Yellow for paid leave
                }
                else if (cellValue == "K" || cellValue == "K/2")
                {
                    cell.Style.Fill.BackgroundColor = XLColor.LightPink; // Pink for unpaid leave/absent
                }
                else if (cellValue == "L")
                {
                    cell.Style.Fill.BackgroundColor = XLColor.Cyan; // Cyan for holidays
                }

                col++;
            }

            // Summary cells
            worksheet.Cell(rowNum, col++).Value = totalCong;
            worksheet.Cell(rowNum, col++).Value = totalP;
            worksheet.Cell(rowNum, col++).Value = totalK;
            worksheet.Cell(rowNum, col++).Value = totalHolidays;

            rowNum++;
        }

        // Auto-fit columns
        for (int i = 1; i <= 4; i++)
            worksheet.Column(i).AdjustToContents();

        // Set fixed width for date columns (compact display)
        for (int i = 5; i < col - 4; i++)
            worksheet.Column(i).Width = 8;

        // Auto-fit summary columns
        for (int i = col - 4; i < col; i++)
            worksheet.Column(i).AdjustToContents();

        // Return as byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private string GetCellValue(DateOnly date, Models.Entities.Attendance? att, List<Models.Entities.Request> requests,
        HashSet<DateOnly> holidays, TimeOnly morningStart, TimeOnly morningEnd, TimeOnly afternoonStart, TimeOnly afternoonEnd,
        int employeeId, HashSet<int> fullAttendanceEmployeeIds,
        out decimal dayCong, out decimal dayP, out decimal dayK, out decimal dayHoliday)
    {
        dayCong = 0; dayP = 0; dayK = 0; dayHoliday = 0;

        // 0. Check if employee is in full attendance list  
        if (fullAttendanceEmployeeIds.Contains(employeeId) && IsWorkingDayForExport(date))
        {
            dayCong = 1;
            return "1";
        }

        // 1. Holiday
        if (holidays.Contains(date))
        {
            dayHoliday = 1;
            return "L";
        }

        // 2. Non-working day (Sunday, non-work Saturday) - use AttendanceCalendarViewModel logic
        if (!IsWorkingDayForExport(date))
        {
            return "";
        }

        // Get requests affecting this date
        var dayRequests = GetRequestsForDate(requests, date);
        var approvedRequests = dayRequests.Where(r => r.request.Status == Models.Enums.RequestStatus.FullyApproved).ToList();

        // 3. Check for full attendance (both shifts covered)
        if (att?.CheckIn != null && att.CheckOut != null)
        {
            var minAfternoonCheckOut = AttendanceCalendarViewModel.GetMinAfternoonCheckOut(date);
            bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
            bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= minAfternoonCheckOut;

            if (hasMorning && hasAfternoon)
            {
                dayCong = 1;
                return "1";
            }
        }

        // 4. Check approved CountsAsWork requests (non-Leave types: Absence, WorkFromHome, CheckInOut)
        var countsAsWorkNonLeave = approvedRequests
            .Where(r => r.request.RequestType != Models.Enums.RequestType.Leave && r.request.CountsAsWork)
            .FirstOrDefault();

        if (countsAsWorkNonLeave.request != null)
        {
            bool isHalf = countsAsWorkNonLeave.isHalf;
            dayCong = isHalf ? 0.5m : 1;
            return isHalf ? "1/2" : "1";
        }

        // 5. Check approved Leave requests with CountsAsWork = true (paid leave reasons)
        var paidLeaveReq = approvedRequests
            .Where(r => r.request.RequestType == Models.Enums.RequestType.Leave && r.request.CountsAsWork)
            .FirstOrDefault();

        if (paidLeaveReq.request != null)
        {
            bool isHalf = paidLeaveReq.isHalf;
            dayP = isHalf ? 0.5m : 1;
            dayCong = dayP; // Paid leave counts as work
            return isHalf ? "P/2" : "P";
        }

        // 6. Check approved Leave requests with CountsAsWork = false (unpaid leave)
        var unpaidLeaveReq = approvedRequests
            .Where(r => r.request.RequestType == Models.Enums.RequestType.Leave && !r.request.CountsAsWork)
            .FirstOrDefault();

        if (unpaidLeaveReq.request != null)
        {
            bool isHalf = unpaidLeaveReq.isHalf;
            dayK = isHalf ? 0.5m : 1;
            return isHalf ? "K/2" : "K";
        }

        // 7. ForgotCheckInOut with partial attendance = count as full attendance
        var forgotCheckReq = approvedRequests
            .Where(r => r.request.LeaveReason == Models.Enums.LeaveReason.ForgotCheckInOut)
            .FirstOrDefault();

        if (forgotCheckReq.request != null && att != null)
        {
            dayCong = 1;
            return "1";
        }

        // 8. Half attendance without approved request
        if (att?.CheckIn != null)
        {
            dayCong = 0.5m;
            dayK = 0.5m;
            return "K/2";
        }

        // 9. No record on working day
        dayK = 1;
        return "K";
    }

    private List<(Models.Entities.Request request, bool isHalf)> GetRequestsForDate(List<Models.Entities.Request> requests, DateOnly date)
    {
        var result = new List<(Models.Entities.Request request, bool isHalf)>();

        foreach (var req in requests)
        {
            var reqStart = DateOnly.FromDateTime(req.StartTime);
            var reqEnd = DateOnly.FromDateTime(req.EndTime);

            if (date >= reqStart && date <= reqEnd)
            {
                // Determine if this specific date is a half day
                bool isHalf = false;
                if (date == reqStart && req.IsHalfDayStart)
                    isHalf = true;
                else if (date == reqEnd && req.IsHalfDayEnd)
                    isHalf = true;

                result.Add((req, isHalf));
            }
        }

        return result;
    }

    private static bool IsWorkingDayForExport(DateOnly date)
    {
        if (date.DayOfWeek == DayOfWeek.Sunday) return false;
        if (date.DayOfWeek == DayOfWeek.Saturday)
        {
            // Use same logic as AttendanceCalendarViewModel.IsWorkSaturday
            var anchor = new DateOnly(2026, 3, 21);
            var diffWeeks = (date.DayNumber - anchor.DayNumber) / 7;
            return diffWeeks % 2 == 0;
        }
        return true;
    }

    private static (DateOnly periodStart, DateOnly periodEnd) GetPeriodDates(int year, int month)
    {
        var currentMonth = new DateTime(year, month, 1);
        var prevMonth = currentMonth.AddMonths(-1);
        var periodStart = new DateOnly(prevMonth.Year, prevMonth.Month, 26);
        var periodEnd = new DateOnly(currentMonth.Year, currentMonth.Month, 25);
        return (periodStart, periodEnd);
    }

    public async Task<int> UpdateExistingLateMinutesAsync()
    {
        // Get all attendance records that are late but don't have LateMinutes calculated
        var attendancesToUpdate = await _unitOfWork.Attendances
            .FindAsync(a => a.IsLate && a.LateMinutes == 0 && a.CheckIn != null);

        int updatedCount = 0;

        foreach (var attendance in attendancesToUpdate)
        {
            if (attendance.CheckIn.HasValue)
            {
                // Calculate late minutes based on CheckIn time vs LateThreshold (8:30 AM)
                var lateMinutes = 0;
                if (attendance.CheckIn.Value > LateThreshold)
                {
                    lateMinutes = (int)(attendance.CheckIn.Value - LateThreshold).TotalMinutes;
                }

                // Update the record
                attendance.LateMinutes = Math.Max(0, lateMinutes);
                _unitOfWork.Attendances.Update(attendance);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();

            await _logService.LogAsync(
                GetCurrentUserId(),
                "Cập nhật phút đi muộn",
                "Attendance",
                null,
                new { UpdatedCount = updatedCount },
                GetClientIP()
            );
        }

        return updatedCount;
    }

    public async Task<int> RecalculateAllAttendanceTimesAsync()
    {
        // Get all attendance records
        var allAttendances = await _unitOfWork.Attendances.GetAllAsync();
        int updatedCount = 0;

        foreach (var attendance in allAttendances)
        {
            // Get all punch records for this employee and date
            var punchRecords = await _unitOfWork.PunchRecords.GetByEmployeeAndDateAsync(attendance.EmployeeId, attendance.Date);
            var orderedPunches = punchRecords.OrderBy(pr => pr.PunchTime).ToList();

            if (!orderedPunches.Any())
                continue; // No punch records, skip

            var newCheckIn = orderedPunches.First().PunchTime;
            TimeOnly? newCheckOut = null;

            // Only set checkOut if there are multiple punches AND last is different from first
            if (orderedPunches.Count > 1)
            {
                var lastPunchTime = orderedPunches.Last().PunchTime;
                if (lastPunchTime != newCheckIn)
                {
                    newCheckOut = lastPunchTime;
                }
            }

            // Check if times changed
            bool timesChanged = attendance.CheckIn != newCheckIn || attendance.CheckOut != newCheckOut;

            if (timesChanged)
            {
                attendance.CheckIn = newCheckIn;
                attendance.CheckOut = newCheckOut;

                // Recalculate working hours
                if (newCheckOut.HasValue)
                    attendance.WorkingHours = Math.Round((decimal)(newCheckOut.Value.ToTimeSpan() - newCheckIn.ToTimeSpan()).TotalHours, 2);
                else
                    attendance.WorkingHours = null;

                // Recalculate late status
                attendance.IsLate = newCheckIn > LateThreshold;
                attendance.LateMinutes = attendance.IsLate ? (int)(newCheckIn - LateThreshold).TotalMinutes : 0;

                _unitOfWork.Attendances.Update(attendance);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();

            await _logService.LogAsync(
                GetCurrentUserId(),
                "Đồng bộ lại giờ chấm công từ PunchRecords",
                "Attendance",
                null,
                new { UpdatedCount = updatedCount },
                GetClientIP()
            );
        }

        return updatedCount;
    }
}
