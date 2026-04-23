using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace managerCMN.Models.ViewModels;

public class TicketCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    [MaxLength(200)]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    [Display(Name = "Nội dung")]
    public string? Description { get; set; }

    [Display(Name = "Độ ưu tiên")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Display(Name = "Độ khẩn")]
    public TicketUrgency Urgency { get; set; } = TicketUrgency.Normal;

    [Required(ErrorMessage = "Vui lòng nhập deadline")]
    [DataType(DataType.Date)]
    [Display(Name = "Deadline")]
    public DateTime? Deadline { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ít nhất một người nhận")]
    [Display(Name = "Người nhận")]
    public List<int> RecipientIds { get; set; } = new();

    [Display(Name = "Tệp đính kèm")]
    public List<IFormFile>? Attachments { get; set; }

    // For dropdown population
    public List<SelectListItem>? AvailableRecipients { get; set; }
}
