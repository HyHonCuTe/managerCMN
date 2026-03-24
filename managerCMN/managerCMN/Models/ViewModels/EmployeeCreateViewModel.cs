using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class EmergencyContactInput
{
    [MaxLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự")]
    public string? FullName { get; set; }

    [MaxLength(50, ErrorMessage = "Mối quan hệ không được vượt quá 50 ký tự")]
    public string? Relationship { get; set; }

    [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678)")]
    public string? Phone { get; set; }

    [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    public string? Address { get; set; }
}

public class EmployeeCreateViewModel
{
    [MaxLength(20, ErrorMessage = "Mã nhân viên không được vượt quá 20 ký tự")]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã nhân viên chỉ chứa chữ hoa và số (VD: A00409)")]
    public string? EmployeeCode { get; set; }

    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự")]
    [MinLength(2, ErrorMessage = "Họ tên phải có ít nhất 2 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Giới tính là bắt buộc")]
    public Gender Gender { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (VD: 0912345678)")]
    public string? Phone { get; set; }

    [MaxLength(50, ErrorMessage = "Tên chấm công không được vượt quá 50 ký tự")]
    public string? AttendanceName { get; set; }

    [MaxLength(20, ErrorMessage = "Mã chấm công không được vượt quá 20 ký tự")]
    public string? AttendanceCode { get; set; }

    [MaxLength(500, ErrorMessage = "Địa chỉ thường trú không được vượt quá 500 ký tự")]
    public string? PermanentAddress { get; set; }

    [MaxLength(500, ErrorMessage = "Địa chỉ liên hệ không được vượt quá 500 ký tự")]
    public string? TemporaryAddress { get; set; }

    [MaxLength(20, ErrorMessage = "Mã số thuế không được vượt quá 20 ký tự")]
    [RegularExpression(@"^[0-9]{10}(-[0-9]{3})?$", ErrorMessage = "Mã số thuế không hợp lệ (VD: 0123456789 hoặc 0123456789-001)")]
    public string? TaxCode { get; set; }

    [MaxLength(50, ErrorMessage = "Số tài khoản không được vượt quá 50 ký tự")]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "Số tài khoản chỉ chứa số")]
    public string? BankAccount { get; set; }

    [MaxLength(100, ErrorMessage = "Tên ngân hàng không được vượt quá 100 ký tự")]
    public string? BankName { get; set; }

    public int? DepartmentId { get; set; }

    public int? JobTitleId { get; set; }

    public int? PositionId { get; set; }

    [MaxLength(50, ErrorMessage = "Dân tộc không được vượt quá 50 ký tự")]
    public string? Ethnicity { get; set; }

    [MaxLength(50, ErrorMessage = "Quốc tịch không được vượt quá 50 ký tự")]
    public string? Nationality { get; set; }

    [MaxLength(20, ErrorMessage = "Số CCCD không được vượt quá 20 ký tự")]
    [RegularExpression(@"^[0-9]{9}$|^[0-9]{12}$", ErrorMessage = "Số CCCD phải là 9 hoặc 12 số")]
    public string? IdCardNumber { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? IdCardIssueDate { get; set; }

    [MaxLength(200, ErrorMessage = "Nơi cấp không được vượt quá 200 ký tự")]
    public string? IdCardIssuePlace { get; set; }

    [MaxLength(2000, ErrorMessage = "Bằng cấp không được vượt quá 2000 ký tự")]
    public string? Qualifications { get; set; }

    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime? StartWorkingDate { get; set; }

    [MaxLength(20, ErrorMessage = "Mã BHXH không được vượt quá 20 ký tự")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mã BHXH phải là 10 số")]
    public string? InsuranceCode { get; set; }

    [MaxLength(100, ErrorMessage = "Số xe không được vượt quá 100 ký tự")]
    [RegularExpression(@"^[0-9]{2}[A-Z]{1,2}-[0-9]{4,5}(\.[0-9]{2})?$", ErrorMessage = "Biển số xe không hợp lệ (VD: 29A-12345)")]
    public string? VehiclePlate { get; set; }

    [MaxLength(500, ErrorMessage = "Facebook URL không được vượt quá 500 ký tự")]
    [Url(ErrorMessage = "Facebook URL không hợp lệ")]
    public string? FacebookUrl { get; set; }

    public List<EmergencyContactInput> EmergencyContacts { get; set; } = new();
}
