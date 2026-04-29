using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class TicketAttachment
{
    [Key]
    public int TicketAttachmentId { get; set; }

    // Can belong to a ticket (initial) or a message (reply/forward)
    public int? TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public int? TicketMessageId { get; set; }
    public TicketMessage? TicketMessage { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public int UploadedById { get; set; }
    public Employee UploadedBy { get; set; } = null!;

    public DateTime UploadedDate { get; set; } = DateTimeHelper.VietnamNow;
}
