using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Enums;

public enum MeetingRoomBookingStatus
{
    [Display(Name = "Đã đặt")]
    Scheduled = 0,

    [Display(Name = "Đã hủy")]
    Cancelled = 1
}
