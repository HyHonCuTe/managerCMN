using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class MeetingRoomBooking
{
    [Key]
    public int MeetingRoomBookingId { get; set; }

    [Required]
    public int MeetingRoomId { get; set; }
    public MeetingRoom MeetingRoom { get; set; } = null!;

    [Required]
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public MeetingRoomBookingStatus Status { get; set; } = MeetingRoomBookingStatus.Scheduled;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.VietnamNow;

    public DateTime? ModifiedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}
