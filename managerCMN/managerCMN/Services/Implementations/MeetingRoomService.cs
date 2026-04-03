using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using managerCMN.Helpers;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class MeetingRoomService : IMeetingRoomService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MeetingRoomService(
        IUnitOfWork unitOfWork,
        ISystemLogService logService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyList<MeetingRoom>> GetActiveRoomsAsync()
        => (await _unitOfWork.MeetingRooms.GetActiveAsync()).ToList();

    public async Task<IReadOnlyList<MeetingRoom>> GetAllRoomsAsync()
        => (await _unitOfWork.MeetingRooms.GetAllOrderedAsync()).ToList();

    public async Task<IReadOnlyList<MeetingRoomBooking>> GetBookingsByDateAsync(DateTime selectedDate)
        => (await _unitOfWork.MeetingRoomBookings.GetByDateAsync(selectedDate)).ToList();

    public async Task<IReadOnlyList<MeetingRoomBooking>> GetUpcomingBookingsAsync(int employeeId, bool isAdmin, int take = 10)
        => (await _unitOfWork.MeetingRoomBookings.GetUpcomingAsync(employeeId, isAdmin, take)).ToList();

    public async Task CreateRoomAsync(MeetingRoom room)
    {
        room.Name = room.Name.Trim();
        room.Location = string.IsNullOrWhiteSpace(room.Location) ? null : room.Location.Trim();
        room.ColorHex = NormalizeColor(room.ColorHex);

        if (await _unitOfWork.MeetingRooms.NameExistsAsync(room.Name))
        {
            throw new ValidationException("Tên phòng họp đã tồn tại.");
        }

        room.CreatedAt = DateTimeHelper.VietnamNow;
        await _unitOfWork.MeetingRooms.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo phòng họp",
            "MeetingRoom",
            null,
            new { room.MeetingRoomId, room.Name, room.Location, room.Capacity, room.ColorHex, room.IsActive },
            GetClientIp());
    }

    public async Task UpdateRoomStatusAsync(int roomId, bool isActive)
    {
        var room = await _unitOfWork.MeetingRooms.GetByIdAsync(roomId)
            ?? throw new ValidationException("Không tìm thấy phòng họp.");

        if (!isActive)
        {
            var hasFutureBookings = await _unitOfWork.MeetingRoomBookings
                .HasFutureScheduledBookingsAsync(roomId, DateTimeHelper.VietnamNow);

            if (hasFutureBookings)
            {
                throw new ValidationException("Không thể ngưng sử dụng phòng đang có lịch họp sắp tới.");
            }
        }

        var before = new { room.MeetingRoomId, room.Name, room.IsActive };
        room.IsActive = isActive;
        room.ModifiedAt = DateTimeHelper.VietnamNow;

        _unitOfWork.MeetingRooms.Update(room);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            isActive ? "Kích hoạt phòng họp" : "Ngưng sử dụng phòng họp",
            "MeetingRoom",
            before,
            new { room.MeetingRoomId, room.Name, room.IsActive },
            GetClientIp());
    }

    public async Task CreateBookingAsync(MeetingRoomBooking booking)
    {
        ValidateBooking(booking);

        var room = await _unitOfWork.MeetingRooms.GetByIdAsync(booking.MeetingRoomId)
            ?? throw new ValidationException("Phòng họp không tồn tại.");

        if (!room.IsActive)
        {
            throw new ValidationException("Phòng họp này hiện không khả dụng để đặt lịch.");
        }

        var employee = await _unitOfWork.Employees.GetByIdAsync(booking.EmployeeId)
            ?? throw new ValidationException("Không tìm thấy nhân viên đặt phòng.");

        var hasOverlap = await _unitOfWork.MeetingRoomBookings
            .HasOverlapAsync(booking.MeetingRoomId, booking.StartTime, booking.EndTime);

        if (hasOverlap)
        {
            throw new ValidationException("Khung giờ này đã có người đặt. Vui lòng chọn thời gian khác.");
        }

        booking.Title = booking.Title.Trim();
        booking.Description = string.IsNullOrWhiteSpace(booking.Description) ? null : booking.Description.Trim();
        booking.CreatedAt = DateTimeHelper.VietnamNow;
        booking.Status = MeetingRoomBookingStatus.Scheduled;

        await _unitOfWork.MeetingRoomBookings.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Đặt phòng họp",
            "MeetingRoomBooking",
            null,
            new
            {
                booking.MeetingRoomBookingId,
                booking.MeetingRoomId,
                RoomName = room.Name,
                booking.EmployeeId,
                EmployeeName = employee.FullName,
                booking.Title,
                booking.StartTime,
                booking.EndTime
            },
            GetClientIp());
    }

    public async Task CancelBookingAsync(int bookingId, int employeeId, bool isAdmin)
    {
        var booking = await _unitOfWork.MeetingRoomBookings.GetWithDetailsAsync(bookingId)
            ?? throw new ValidationException("Không tìm thấy lịch đặt phòng.");

        if (booking.Status == MeetingRoomBookingStatus.Cancelled)
        {
            return;
        }

        if (!isAdmin && booking.EmployeeId != employeeId)
        {
            throw new ValidationException("Bạn không có quyền hủy lịch đặt phòng này.");
        }

        if (!isAdmin && booking.StartTime <= DateTimeHelper.VietnamNow)
        {
            throw new ValidationException("Lịch họp đã tới giờ bắt đầu. Chỉ admin mới có thể hủy lịch này.");
        }

        var before = new
        {
            booking.MeetingRoomBookingId,
            booking.MeetingRoomId,
            RoomName = booking.MeetingRoom.Name,
            booking.EmployeeId,
            EmployeeName = booking.Employee.FullName,
            booking.Title,
            booking.StartTime,
            booking.EndTime,
            booking.Status
        };

        booking.Status = MeetingRoomBookingStatus.Cancelled;
        booking.CancelledAt = DateTimeHelper.VietnamNow;
        booking.ModifiedAt = booking.CancelledAt;

        _unitOfWork.MeetingRoomBookings.Update(booking);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Hủy lịch phòng họp",
            "MeetingRoomBooking",
            before,
            new
            {
                booking.MeetingRoomBookingId,
                booking.Status,
                booking.CancelledAt
            },
            GetClientIp());
    }

    private static void ValidateBooking(MeetingRoomBooking booking)
    {
        if (booking.MeetingRoomId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng họp.");
        }

        if (booking.EmployeeId <= 0)
        {
            throw new ValidationException("Không xác định được nhân viên đặt phòng.");
        }

        if (string.IsNullOrWhiteSpace(booking.Title))
        {
            throw new ValidationException("Vui lòng nhập tiêu đề cuộc họp.");
        }

        if (booking.EndTime <= booking.StartTime)
        {
            throw new ValidationException("Giờ kết thúc phải sau giờ bắt đầu.");
        }

        if (booking.StartTime.Date != booking.EndTime.Date)
        {
            throw new ValidationException("Lịch họp chỉ được đặt trong cùng một ngày.");
        }

        if (booking.StartTime < DateTimeHelper.VietnamNow.AddMinutes(30))
        {
            throw new ValidationException("Bạn phải đặt phòng trước ít nhất 30 phút so với giờ bắt đầu.");
        }
    }

    private static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#f97316";
        }

        var normalized = color.Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(normalized, "^#([0-9a-fA-F]{6})$")
            ? normalized
            : "#f97316";
    }

    private int? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetClientIp()
        => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}
