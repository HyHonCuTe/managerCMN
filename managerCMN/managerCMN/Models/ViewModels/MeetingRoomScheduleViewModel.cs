using System.ComponentModel.DataAnnotations;
using System.Globalization;
using managerCMN.Helpers;
using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class MeetingRoomScheduleViewModel
{
    public DateTime SelectedDate { get; set; } = DateTimeHelper.VietnamToday;

    public bool IsAdmin { get; set; }

    public List<MeetingRoomColumnViewModel> Rooms { get; set; } = [];

    public List<MeetingRoom> AllRooms { get; set; } = [];

    public List<MeetingRoomBookingSummaryViewModel> UpcomingBookings { get; set; } = [];

    public MeetingRoomBookingCreateViewModel NewBooking { get; set; } = new();

    public MeetingRoomCreateViewModel NewRoom { get; set; } = new();

    public TimeSpan DayStart { get; set; } = new(8, 30, 0);

    public TimeSpan DayEnd { get; set; } = new(17, 30, 0);

    public int SlotMinutes { get; set; } = 30;

    public int SlotCount => (int)Math.Ceiling((DayEnd - DayStart).TotalMinutes / SlotMinutes);

    public IEnumerable<MeetingTimeSlotViewModel> TimeSlots
    {
        get
        {
            for (var i = 0; i < SlotCount; i++)
            {
                var start = DayStart.Add(TimeSpan.FromMinutes(i * SlotMinutes));
                var end = start.Add(TimeSpan.FromMinutes(SlotMinutes));
                if (end > DayEnd)
                {
                    end = DayEnd;
                }

                yield return new MeetingTimeSlotViewModel
                {
                    Index = i,
                    Start = start,
                    End = end
                };
            }
        }
    }

    public string SelectedDateDisplay => SelectedDate.ToString("dddd, dd/MM/yyyy", new CultureInfo("vi-VN"));

    public int TotalRooms => Rooms.Count;

    public int TotalBookings => Rooms.Sum(r => r.Bookings.Count);

    public int GetGridRowStart(DateTime startTime)
    {
        var diffMinutes = Math.Max(0, (int)(startTime.TimeOfDay - DayStart).TotalMinutes);
        return (diffMinutes / SlotMinutes) + 1;
    }

    public int GetGridRowEnd(DateTime endTime)
    {
        var diffMinutes = Math.Min((int)(DayEnd - DayStart).TotalMinutes, (int)(endTime.TimeOfDay - DayStart).TotalMinutes);
        return Math.Max(GetGridRowStart(endTime), (int)Math.Ceiling(diffMinutes / (double)SlotMinutes) + 1);
    }

    public bool IsBookingVisible(DateTime startTime, DateTime endTime)
        => endTime.TimeOfDay > DayStart && startTime.TimeOfDay < DayEnd;

    public int GetVisibleTopMinutes(DateTime startTime)
    {
        var clippedStart = startTime.TimeOfDay < DayStart ? DayStart : startTime.TimeOfDay;
        return Math.Max(0, (int)(clippedStart - DayStart).TotalMinutes);
    }

    public int GetVisibleDurationMinutes(DateTime startTime, DateTime endTime)
    {
        var clippedStart = startTime.TimeOfDay < DayStart ? DayStart : startTime.TimeOfDay;
        var clippedEnd = endTime.TimeOfDay > DayEnd ? DayEnd : endTime.TimeOfDay;
        return Math.Max(1, (int)Math.Ceiling((clippedEnd - clippedStart).TotalMinutes));
    }
}

public class MeetingRoomColumnViewModel
{
    public MeetingRoom Room { get; set; } = null!;

    public List<MeetingRoomBooking> Bookings { get; set; } = [];
}

public class MeetingTimeSlotViewModel
{
    public int Index { get; set; }

    public TimeSpan Start { get; set; }

    public TimeSpan End { get; set; }

    public string Label => Start.ToString(@"hh\:mm");
}

public class MeetingRoomBookingCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn phòng họp.")]
    public int MeetingRoomId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày họp.")]
    [DataType(DataType.Date)]
    public DateTime BookingDate { get; set; } = DateTimeHelper.VietnamToday;

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề cuộc họp.")]
    [MaxLength(200, ErrorMessage = "Tiêu đề cuộc họp tối đa 200 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu.")]
    public string StartClock { get; set; } = "09:00";

    [Required(ErrorMessage = "Vui lòng chọn giờ kết thúc.")]
    public string EndClock { get; set; } = "10:00";

    [MaxLength(2000, ErrorMessage = "Mô tả tối đa 2000 ký tự.")]
    public string? Description { get; set; }
}

public class MeetingRoomCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên phòng họp.")]
    [MaxLength(100, ErrorMessage = "Tên phòng họp tối đa 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150, ErrorMessage = "Vị trí tối đa 150 ký tự.")]
    public string? Location { get; set; }

    [Range(1, 200, ErrorMessage = "Sức chứa phải từ 1 đến 200 người.")]
    public int? Capacity { get; set; }

    [RegularExpression("^#([0-9a-fA-F]{6})$", ErrorMessage = "Màu sắc phải theo định dạng HEX, ví dụ #f97316.")]
    public string ColorHex { get; set; } = "#f97316";
}

public class MeetingRoomBookingSummaryViewModel
{
    public MeetingRoomBooking Booking { get; set; } = null!;

    public bool CanCancel { get; set; }

    public string TimeDisplay =>
        $"{Booking.StartTime:dd/MM/yyyy} | {Booking.StartTime:HH:mm} - {Booking.EndTime:HH:mm}";
}
