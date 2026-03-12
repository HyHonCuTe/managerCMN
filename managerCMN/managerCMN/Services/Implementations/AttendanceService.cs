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

        foreach (var row in rows)
        {
            var empCode = row.Cell(1).GetString().Trim();
            var employee = await _unitOfWork.Employees.GetByCodeAsync(empCode);
            if (employee == null) continue;

            var dateValue = row.Cell(2).GetDateTime();
            var date = DateOnly.FromDateTime(dateValue);

            // Check if attendance already exists
            var existing = await _unitOfWork.Attendances
                .AnyAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == date);
            if (existing) continue;

            TimeOnly? checkIn = null;
            TimeOnly? checkOut = null;

            if (!row.Cell(3).IsEmpty())
                checkIn = TimeOnly.FromDateTime(row.Cell(3).GetDateTime());

            if (!row.Cell(4).IsEmpty())
                checkOut = TimeOnly.FromDateTime(row.Cell(4).GetDateTime());

            decimal? workingHours = null;
            if (checkIn.HasValue && checkOut.HasValue)
                workingHours = (decimal)(checkOut.Value.ToTimeSpan() - checkIn.Value.ToTimeSpan()).TotalHours;

            var attendance = new Attendance
            {
                EmployeeId = employee.EmployeeId,
                Date = date,
                CheckIn = checkIn,
                CheckOut = checkOut,
                WorkingHours = workingHours,
                OvertimeHours = workingHours > 8 ? workingHours - 8 : 0,
                IsLate = checkIn.HasValue && checkIn.Value > LateThreshold,
                Note = row.Cell(5).IsEmpty() ? null : row.Cell(5).GetString()
            };

            await _unitOfWork.Attendances.AddAsync(attendance);
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
