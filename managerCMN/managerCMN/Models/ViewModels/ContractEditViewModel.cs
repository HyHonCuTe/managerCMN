using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class ContractEditViewModel
{
    public int ContractId { get; set; }

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
