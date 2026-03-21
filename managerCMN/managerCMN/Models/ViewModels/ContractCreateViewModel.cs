using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;
using managerCMN.Attributes;

namespace managerCMN.Models.ViewModels;

public class ContractCreateViewModel
{
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số hợp đồng")]
    [MaxLength(50, ErrorMessage = "Số hợp đồng không quá 50 ký tự")]
    [Display(Name = "Số hợp đồng")]
    public string ContractNumber { get; set; } = string.Empty;

    public ContractType ContractType { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    public decimal Salary { get; set; }

    [ValidateFile(".pdf,.doc,.docx,.txt", false)]
    [Display(Name = "File hợp đồng")]
    public IFormFile? ContractFile { get; set; }
}
