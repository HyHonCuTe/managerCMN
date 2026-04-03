using Microsoft.EntityFrameworkCore;
using managerCMN.Data;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class MeetingRoomBookingRepository : Repository<MeetingRoomBooking>, IMeetingRoomBookingRepository
{
    public MeetingRoomBookingRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<MeetingRoomBooking>> GetByDateAsync(DateTime selectedDate)
    {
        var dayStart = selectedDate.Date;
        var dayEnd = dayStart.AddDays(1);

        return await _dbSet
            .AsNoTracking()
            .Include(b => b.MeetingRoom)
            .Include(b => b.Employee)
                .ThenInclude(e => e.Department)
            .Where(b => b.Status == MeetingRoomBookingStatus.Scheduled
                && b.StartTime < dayEnd
                && b.EndTime > dayStart)
            .OrderBy(b => b.StartTime)
            .ThenBy(b => b.MeetingRoom.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<MeetingRoomBooking>> GetUpcomingAsync(int employeeId, bool isAdmin, int take = 10)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(b => b.MeetingRoom)
            .Include(b => b.Employee)
            .Where(b => b.Status == MeetingRoomBookingStatus.Scheduled);

        if (!isAdmin)
        {
            query = query.Where(b => b.EmployeeId == employeeId);
        }

        return await query
            .OrderBy(b => b.StartTime)
            .Take(take)
            .ToListAsync();
    }

    public async Task<MeetingRoomBooking?> GetWithDetailsAsync(int id)
        => await _dbSet
            .Include(b => b.MeetingRoom)
            .Include(b => b.Employee)
            .FirstOrDefaultAsync(b => b.MeetingRoomBookingId == id);

    public async Task<bool> HasOverlapAsync(int roomId, DateTime startTime, DateTime endTime, int? ignoreBookingId = null)
        => await _dbSet.AnyAsync(b =>
            b.MeetingRoomId == roomId &&
            b.Status == MeetingRoomBookingStatus.Scheduled &&
            (!ignoreBookingId.HasValue || b.MeetingRoomBookingId != ignoreBookingId.Value) &&
            b.StartTime < endTime &&
            startTime < b.EndTime);

    public async Task<bool> HasFutureScheduledBookingsAsync(int roomId, DateTime fromTime)
        => await _dbSet.AnyAsync(b =>
            b.MeetingRoomId == roomId &&
            b.Status == MeetingRoomBookingStatus.Scheduled &&
            b.EndTime >= fromTime);
}
