using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace managerCMN.Models.ViewModels;

public class TicketDetailViewModel
{
    public Ticket Ticket { get; set; } = null!;

    public bool IsCreator { get; set; }
    public bool IsRecipient { get; set; }
    public bool CanReply { get; set; }
    public bool CanForward { get; set; }
    public bool CanUpdateStatus { get; set; }

    public TicketRecipient? CurrentRecipient { get; set; }

    // For reply form
    public string? ReplyContent { get; set; }
    public List<IFormFile>? ReplyAttachments { get; set; }

    // For forward form
    public List<int>? ForwardRecipientIds { get; set; }
    public string? ForwardContent { get; set; }
    public List<IFormFile>? ForwardAttachments { get; set; }
    public List<SelectListItem>? AvailableRecipients { get; set; }
}
