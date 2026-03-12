using System.ComponentModel.DataAnnotations;

namespace managerCMN.Models.Entities;

public class Attendance
{
    [Key]
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly Date { get; set; }

    public TimeOnly? CheckIn { get; set; }

    public TimeOnly? CheckOut { get; set; }

    public decimal? WorkingHours { get; set; }

    public decimal? OvertimeHours { get; set; }

    public bool IsLate { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
