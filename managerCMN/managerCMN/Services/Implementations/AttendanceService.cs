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
}
