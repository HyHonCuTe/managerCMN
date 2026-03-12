using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class RequestAttachment
{
    [Key]
    public int AttachmentId { get; set; }

    public int RequestId { get; set; }
    public Request Request { get; set; } = null!;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
