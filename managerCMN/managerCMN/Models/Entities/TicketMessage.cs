using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class TicketMessage
{
    [Key]
    public int TicketMessageId { get; set; }

    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public int SenderId { get; set; }
    public Employee Sender { get; set; } = null!;

    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public TicketMessageType MessageType { get; set; } = TicketMessageType.Reply;

    // For forward: who was it forwarded to
    public int? ForwardedToId { get; set; }
    public Employee? ForwardedTo { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    // Attachments for this message
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}
