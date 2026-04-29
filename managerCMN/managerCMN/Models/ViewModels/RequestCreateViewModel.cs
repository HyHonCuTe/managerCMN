using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Enums;
using managerCMN.Attributes;
using managerCMN.Helpers;

namespace managerCMN.Models.ViewModels;

public class RequestCreateViewModel
{
    public int? RequestId { get; set; }

    public RequestType RequestType { get; set; }

    /// <summary>Type of check in/out issue (only for CheckInOut requests)</summary>
    public CheckInOutType? CheckInOutType { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; } = DateTimeHelper.VietnamToday;

    [Required]
    public DateTime EndTime { get; set; } = DateTimeHelper.VietnamToday;

    public DateTime StartDate { get; set; } = DateTimeHelper.VietnamToday;

    public DateTime EndDate { get; set; } = DateTimeHelper.VietnamToday;

    public string? StartClock { get; set; }

    public string? EndClock { get; set; }

    public bool IsHalfDayStart { get; set; }

    public bool IsHalfDayEnd { get; set; }

    /// <summary>0=Cả ngày, 1=Nửa ca sáng, 2=Nửa ca chiều</summary>
    public int HalfDayStartOption { get; set; }

    /// <summary>0=Cả ngày, 1=Nửa ca sáng, 2=Nửa ca chiều</summary>
    public int HalfDayEndOption { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn lý do")]
    public LeaveReason? LeaveReason { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public string? Description { get; set; }

    public bool CountsAsWork { get; set; } = true;

    [Required(ErrorMessage = "Vui lòng chọn người duyệt 1")]
    public int? Approver1Id { get; set; }
    public string? Approver1Name { get; set; }
    public bool NeedsManualApprover1Selection { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn người duyệt 2")]
    public int? Approver2Id { get; set; }

    public List<SelectListItem>? AvailableApprover1s { get; set; }
    public List<SelectListItem>? AvailableApprovers { get; set; }
    public List<SelectListItem>? AvailableReasons { get; set; }

    [ValidateFiles(".pdf,.doc,.docx,.jpg,.jpeg,.png,.gif,.txt", false)]
    [Display(Name = "Tệp đính kèm")]
    public List<IFormFile>? Attachments { get; set; }

    public decimal TotalDays { get; set; }
}
