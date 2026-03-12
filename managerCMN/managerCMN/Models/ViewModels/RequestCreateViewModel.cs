using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class RequestCreateViewModel
{
    public RequestType RequestType { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public List<IFormFile>? Attachments { get; set; }
}
