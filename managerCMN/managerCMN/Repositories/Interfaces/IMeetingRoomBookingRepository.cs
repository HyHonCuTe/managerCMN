using managerCMN.Models.Entities;

namespace managerCMN.Repositories.Interfaces;

public interface IMeetingRoomBookingRepository : IRepository<MeetingRoomBooking>
{
    Task<IEnumerable<MeetingRoomBooking>> GetByDateAsync(DateTime selectedDate);
    Task<IEnumerable<MeetingRoomBooking>> GetUpcomingAsync(int employeeId, bool isAdmin, int take = 10);
    Task<MeetingRoomBooking?> GetWithDetailsAsync(int id);
    Task<bool> HasOverlapAsync(int roomId, DateTime startTime, DateTime endTime, int? ignoreBookingId = null);
    Task<bool> HasFutureScheduledBookingsAsync(int roomId, DateTime fromTime);
}
