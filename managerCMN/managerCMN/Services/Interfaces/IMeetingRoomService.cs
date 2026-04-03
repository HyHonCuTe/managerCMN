using managerCMN.Models.Entities;

namespace managerCMN.Services.Interfaces;

public interface IMeetingRoomService
{
    Task<IReadOnlyList<MeetingRoom>> GetActiveRoomsAsync();
    Task<IReadOnlyList<MeetingRoom>> GetAllRoomsAsync();
    Task<IReadOnlyList<MeetingRoomBooking>> GetBookingsByDateAsync(DateTime selectedDate);
    Task<IReadOnlyList<MeetingRoomBooking>> GetUpcomingBookingsAsync(int employeeId, bool isAdmin, int take = 10);
    Task CreateRoomAsync(MeetingRoom room);
    Task UpdateRoomStatusAsync(int roomId, bool isActive);
    Task CreateBookingAsync(MeetingRoomBooking booking);
    Task CancelBookingAsync(int bookingId, int employeeId, bool isAdmin);
}
