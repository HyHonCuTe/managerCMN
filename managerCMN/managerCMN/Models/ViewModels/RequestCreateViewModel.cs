using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using managerCMN.Models.Enums;

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
    public DateTime StartTime { get; set; } = DateTime.Today;

    [Required]
    public DateTime EndTime { get; set; } = DateTime.Today;

    public bool IsHalfDayStart { get; set; }

    public bool IsHalfDayEnd { get; set; }

    /// <summary>0=Cả ngày, 1=Nửa ca sáng, 2=Nửa ca chiều</summary>
    public int HalfDayStartOption { get; set; }

    /// <summary>0=Cả ngày, 1=Nửa ca sáng, 2=Nửa ca chiều</summary>
    public int HalfDayEndOption { get; set; }

    public LeaveReason? LeaveReason { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public string? Description { get; set; }

    public int? Approver1Id { get; set; }
    public string? Approver1Name { get; set; }
    public bool NeedsManualApprover1Selection { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn người duyệt 2")]
    public int Approver2Id { get; set; }

    public List<SelectListItem>? AvailableApprover1s { get; set; }
    public List<SelectListItem>? AvailableApprovers { get; set; }
    public List<SelectListItem>? AvailableReasons { get; set; }

    public List<IFormFile>? Attachments { get; set; }

    public decimal TotalDays { get; set; }
}
