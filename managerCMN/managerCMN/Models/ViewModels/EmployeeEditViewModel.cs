using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class EmployeeEditViewModel
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? PermanentAddress { get; set; }

    [MaxLength(500)]
    public string? TemporaryAddress { get; set; }

    [MaxLength(20)]
    public string? TaxCode { get; set; }

    [MaxLength(50)]
    public string? BankAccount { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    public int? DepartmentId { get; set; }

    [MaxLength(100)]
    public string? Position { get; set; }

    [MaxLength(2000)]
    public string? Qualifications { get; set; }

    public DateTime? StartWorkingDate { get; set; }

    public EmployeeStatus Status { get; set; }
}
