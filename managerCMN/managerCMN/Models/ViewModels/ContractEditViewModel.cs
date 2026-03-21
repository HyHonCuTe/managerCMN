using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class ContractEditViewModel
{
    public int ContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số hợp đồng")]
    [MaxLength(50, ErrorMessage = "Số hợp đồng không quá 50 ký tự")]
    [Display(Name = "Số hợp đồng")]
    public string ContractNumber { get; set; } = string.Empty;

    [Required]
    public int EmployeeId { get; set; }

    public ContractType ContractType { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Salary { get; set; }

    public ContractStatus Status { get; set; }
}
