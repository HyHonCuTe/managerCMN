using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class MeetingRoom
{
    [Key]
    public int MeetingRoomId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Location { get; set; }

    public int? Capacity { get; set; }

    [Required, MaxLength(7)]
    public string ColorHex { get; set; } = "#f97316";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.VietnamNow;

    public DateTime? ModifiedAt { get; set; }

    public ICollection<MeetingRoomBooking> Bookings { get; set; } = new List<MeetingRoomBooking>();
}
