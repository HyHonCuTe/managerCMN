using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

public class ProjectTaskAttachment
{
    public int ProjectTaskAttachmentId { get; set; }

    public int ProjectTaskUpdateId { get; set; }
    public ProjectTaskUpdate ProjectTaskUpdate { get; set; } = null!;

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public int UploadedByEmployeeId { get; set; }
    public Employee UploadedByEmployee { get; set; } = null!;

    public DateTime UploadedDate { get; set; } = DateTimeHelper.VietnamNow;
}
