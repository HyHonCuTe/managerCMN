using ClosedXML.Excel;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly TimeOnly LateThreshold = new(8, 30); // 8:30 AM

    public AttendanceService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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

            if (!empCache.ContainsKey(attCode))
                empCache[attCode] = await _unitOfWork.Employees.GetByAttendanceCodeAsync(attCode);

            var employee = empCache[attCode];
            if (employee == null) continue;

            var existing = await _unitOfWork.Attendances
                .AnyAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == date);
            if (existing) continue;

            var punches = group.OrderBy(r => r.PunchTime).ToList();
            var checkIn = TimeOnly.FromDateTime(punches.First().PunchTime);
            TimeOnly? checkOut = punches.Count > 1 ? TimeOnly.FromDateTime(punches.Last().PunchTime) : null;

            decimal? workingHours = null;
            if (checkOut.HasValue)
                workingHours = Math.Round((decimal)(checkOut.Value.ToTimeSpan() - checkIn.ToTimeSpan()).TotalHours, 2);

            var attendance = new Attendance
            {
                EmployeeId = employee.EmployeeId,
                Date = date,
                CheckIn = checkIn,
                CheckOut = checkOut,
                WorkingHours = workingHours,
                OvertimeHours = 0,
                IsLate = checkIn > LateThreshold,
            };

            await _unitOfWork.Attendances.AddAsync(attendance);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<byte[]> ExportToExcelAsync(int year, int month)
    {
        var (periodStart, periodEnd) = GetPeriodDates(year, month);

        // Get holidays for the period
        var holidays = (await _unitOfWork.Holidays.GetByDateRangeAsync(periodStart, periodEnd))
            .Select(h => h.Date)
            .ToHashSet();

        // Get all employees
        var employees = (await _unitOfWork.Employees.GetAllAsync())
            .OrderBy(e => e.Department?.DepartmentName).ThenBy(e => e.FullName)
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

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bảng chấm công");

        // Shift constants
        var morningStart = new TimeOnly(8, 30);
        var morningEnd = new TimeOnly(12, 0);
        var afternoonStart = new TimeOnly(13, 30);
        var afternoonEnd = new TimeOnly(17, 30);

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
        out decimal dayCong, out decimal dayP, out decimal dayK, out decimal dayHoliday)
    {
        dayCong = 0; dayP = 0; dayK = 0; dayHoliday = 0;

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
            bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
            bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= afternoonStart;

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
}
