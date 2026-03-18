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
        
        // Get all employees
        var employees = (await _unitOfWork.Employees.GetAllAsync())
            .OrderBy(e => e.Department?.DepartmentName).ThenBy(e => e.FullName)
            .ToList();
        
        // Get all attendance for the period
        var attendances = await GetByDateRangeAsync(periodStart, periodEnd);
        var attendanceDict = attendances
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.Date));

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bảng chấm công");
        
        // Constants
        var morningStart = new TimeOnly(8, 30);
        var morningEnd = new TimeOnly(12, 0);
        var afternoonStart = new TimeOnly(13, 30);
        var afternoonEnd = new TimeOnly(17, 30);
        
        // Header row
        int col = 1;
        worksheet.Cell(1, col++).Value = "#";
        worksheet.Cell(1, col++).Value = "Mã NV";
        worksheet.Cell(1, col++).Value = "Thông tin";
        
        // Date columns
        var dateColumns = new List<DateOnly>();
        for (var d = periodStart; d <= periodEnd; d = d.AddDays(1))
        {
            dateColumns.Add(d);
            worksheet.Cell(1, col++).Value = d.ToString("dd/MM");
        }
        
        // Summary columns
        worksheet.Cell(1, col++).Value = "Tổng công";
        worksheet.Cell(1, col++).Value = "Đơn có phép";
        worksheet.Cell(1, col++).Value = "Đơn không phép";
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
            
            decimal totalCong = 0;
            decimal donCoPhep = 0;
            decimal donKhongPhep = 0;
            decimal ngayLe = 0;
            
            var empAttendances = attendanceDict.ContainsKey(emp.EmployeeId) 
                ? attendanceDict[emp.EmployeeId] 
                : new Dictionary<DateOnly, Attendance>();
            
            // Get employee requests
            var empRequests = (await _unitOfWork.Requests.GetByEmployeeAsync(emp.EmployeeId)).ToList();
            var requestsByDate = new Dictionary<DateOnly, List<string>>();
            
            foreach (var req in empRequests)
            {
                if (req.Status == Models.Enums.RequestStatus.Rejected || 
                    req.Status == Models.Enums.RequestStatus.Cancelled)
                    continue;
                    
                var reqStart = DateOnly.FromDateTime(req.StartTime);
                var reqEnd = DateOnly.FromDateTime(req.EndTime);
                
                for (var d = reqStart; d <= reqEnd; d = d.AddDays(1))
                {
                    if (d >= periodStart && d <= periodEnd)
                    {
                        if (!requestsByDate.ContainsKey(d))
                            requestsByDate[d] = new List<string>();
                            
                        string reqText = "Đơn ";
                        if (req.RequestType == Models.Enums.RequestType.Leave)
                            reqText += req.CountsAsWork ? "có phép" : "không phép";
                        else if (req.RequestType == Models.Enums.RequestType.CheckInOut)
                            reqText += "IN/OUT";
                        else if (req.RequestType == Models.Enums.RequestType.Absence)
                            reqText += "vắng mặt";
                            
                        requestsByDate[d].Add(reqText);
                        
                        // Count requests
                        if (req.Status == Models.Enums.RequestStatus.FullyApproved && IsWorkingDay(d))
                        {
                            if (req.RequestType == Models.Enums.RequestType.Leave)
                            {
                                decimal dayValue = req.IsHalfDayStart || req.IsHalfDayEnd ? 0.5m : 1m;
                                
                                if (req.CountsAsWork)
                                    donCoPhep += dayValue;
                                else
                                    donKhongPhep += dayValue;
                            }
                        }
                    }
                }
            }
            
            // Date cells
            foreach (var date in dateColumns)
            {
                var cellValue = "";
                decimal dayCong = 0;
                
                if (empAttendances.ContainsKey(date))
                {
                    var att = empAttendances[date];
                    if (att.CheckIn.HasValue && att.CheckOut.HasValue)
                    {
                        bool hasMorning = att.CheckIn.Value <= morningEnd && att.CheckOut.Value >= morningStart;
                        bool hasAfternoon = att.CheckIn.Value <= afternoonEnd && att.CheckOut.Value >= afternoonStart;
                        
                        if (hasMorning && hasAfternoon)
                        {
                            cellValue = $"1 công\n{att.CheckIn.Value:HH:mm:ss}\n{att.CheckOut.Value:HH:mm:ss}";
                            dayCong = 1;
                        }
                        else if (hasMorning || hasAfternoon)
                        {
                            cellValue = $"0.5 công\n{att.CheckIn.Value:HH:mm:ss}\n{att.CheckOut.Value:HH:mm:ss}";
                            dayCong = 0.5m;
                        }
                    }
                    else if (att.CheckIn.HasValue)
                    {
                        cellValue = $"0.5 công\n{att.CheckIn.Value:HH:mm:ss}\n{att.CheckIn.Value:HH:mm:ss}";
                        dayCong = 0.5m;
                    }
                }
                
                // Add request info if exists
                if (requestsByDate.ContainsKey(date))
                {
                    foreach (var reqText in requestsByDate[date])
                    {
                        if (!string.IsNullOrEmpty(cellValue)) cellValue += "\n";
                        cellValue += reqText;
                    }
                }
                
                if (string.IsNullOrEmpty(cellValue))
                    cellValue = "N";
                    
                worksheet.Cell(rowNum, col).Value = cellValue;
                worksheet.Cell(rowNum, col).Style.Alignment.WrapText = true;
                worksheet.Cell(rowNum, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                col++;
                
                totalCong += dayCong;
            }
            
            // Summary cells
            worksheet.Cell(rowNum, col++).Value = totalCong;
            worksheet.Cell(rowNum, col++).Value = donCoPhep;
            worksheet.Cell(rowNum, col++).Value = donKhongPhep;
            worksheet.Cell(rowNum, col++).Value = ngayLe;
            
            rowNum++;
        }
        
        // Auto-fit columns (limited width for date columns)
        for (int i = 1; i <= 3; i++)
            worksheet.Column(i).AdjustToContents();
        
        for (int i = 4; i < col - 4; i++)
            worksheet.Column(i).Width = 15;
            
        for (int i = col - 4; i < col; i++)
            worksheet.Column(i).AdjustToContents();
        
        // Return as byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    
    private static (DateOnly periodStart, DateOnly periodEnd) GetPeriodDates(int year, int month)
    {
        var currentMonth = new DateTime(year, month, 1);
        var prevMonth = currentMonth.AddMonths(-1);
        var periodStart = new DateOnly(prevMonth.Year, prevMonth.Month, 26);
        var periodEnd = new DateOnly(currentMonth.Year, currentMonth.Month, 25);
        return (periodStart, periodEnd);
    }
    
    private static bool IsWorkingDay(DateOnly date)
    {
        var dow = date.DayOfWeek;
        return dow != DayOfWeek.Saturday && dow != DayOfWeek.Sunday;
    }
}
