using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class EmployeeEditViewModel
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678)")]
    public string? Phone { get; set; }

    [MaxLength(50)]
    public string? AttendanceName { get; set; }

    [MaxLength(20)]
    public string? AttendanceCode { get; set; }

    [MaxLength(500)]
    public string? PermanentAddress { get; set; }

    [MaxLength(500)]
    public string? TemporaryAddress { get; set; }

    [MaxLength(12, ErrorMessage = "Mã số thuế không được vượt quá 12 ký tự")]
    [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "Mã số thuế phải là 12 chữ số")]
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

    [MaxLength(12)]
    [RegularExpression(@"^[0-9]{9}$|^[0-9]{12}$", ErrorMessage = "Số CCCD phải là 9 hoặc 12 số")]
    public string? IdCardNumber { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? IdCardIssueDate { get; set; }

    [MaxLength(200)]
    public string? IdCardIssuePlace { get; set; }

    [MaxLength(2000)]
    public string? Qualifications { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? StartWorkingDate { get; set; }

    [MaxLength(20)]
    public string? InsuranceCode { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? ResignationDate { get; set; }

    [MaxLength(500)]
    public string? ResignationReason { get; set; }

    [MaxLength(100)]
    public string? VehiclePlate { get; set; }

    [MaxLength(500)]
    public string? FacebookUrl { get; set; }

    public EmployeeStatus Status { get; set; }
}
