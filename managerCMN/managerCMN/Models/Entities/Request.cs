using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class Request
{
    [Key]
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public RequestType RequestType { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public int? ApproverId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedDate { get; set; }

    // Navigation
    public ICollection<RequestAttachment> Attachments { get; set; } = new List<RequestAttachment>();
}
