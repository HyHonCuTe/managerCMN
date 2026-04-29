using System.ComponentModel.DataAnnotations;
using managerCMN.Helpers;

namespace managerCMN.Models.Entities;

/// <summary>
/// Danh sách nhân viên được tính công đầy đủ tự động (không cần kiểm tra check-in/out)
/// </summary>
public class FullAttendanceEmployee
{
    [Key]
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    [MaxLength(500)]
    public string? Reason { get; set; } // Lý do (tuỳ chọn)

    public DateTime CreatedDate { get; set; } = DateTimeHelper.VietnamNow;
    public DateTime? UpdatedDate { get; set; }
}
