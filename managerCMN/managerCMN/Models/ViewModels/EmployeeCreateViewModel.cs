using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class EmergencyContactInput
{
    [MaxLength(200)]
    public string? FullName { get; set; }
    [MaxLength(50)]
    public string? Relationship { get; set; }
    [MaxLength(20)]
    public string? Phone { get; set; }
    [MaxLength(500)]
    public string? Address { get; set; }
}

public class EmployeeCreateViewModel
{
    [MaxLength(20)]
    public string? EmployeeCode { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? AttendanceName { get; set; }

    [MaxLength(20)]
    public string? AttendanceCode { get; set; }

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

    public int? JobTitleId { get; set; }

    public int? PositionId { get; set; }

    [MaxLength(50)]
    public string? Ethnicity { get; set; }

    [MaxLength(50)]
    public string? Nationality { get; set; }

    [MaxLength(20)]
    public string? IdCardNumber { get; set; }

    public DateTime? IdCardIssueDate { get; set; }

    [MaxLength(200)]
    public string? IdCardIssuePlace { get; set; }

    [MaxLength(2000)]
    public string? Qualifications { get; set; }

    public DateTime? StartWorkingDate { get; set; }

    [MaxLength(20)]
    public string? InsuranceCode { get; set; }

    [MaxLength(100)]
    public string? VehiclePlate { get; set; }

    [MaxLength(500)]
    public string? FacebookUrl { get; set; }

    public List<EmergencyContactInput> EmergencyContacts { get; set; } = new();
}
