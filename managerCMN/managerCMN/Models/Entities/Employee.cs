using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.Entities;

public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required, MaxLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? PermanentAddress { get; set; }

    [MaxLength(500)]
    public string? TemporaryAddress { get; set; }

    [MaxLength(20)]
    public string? AttendanceCode { get; set; }

    [MaxLength(50)]
    public string? AttendanceName { get; set; }

    [MaxLength(12)]
    public string? TaxCode { get; set; }

    [MaxLength(50)]
    public string? BankAccount { get; set; }

    [MaxLength(100)]
    public string? BankName { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? JobTitleId { get; set; }
    public JobTitle? JobTitle { get; set; }

    public int? PositionId { get; set; }
    public Position? Position { get; set; }

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

    public DateTime? ResignationDate { get; set; }

    [MaxLength(500)]
    public string? ResignationReason { get; set; }

    [MaxLength(100)]
    public string? VehiclePlate { get; set; }

    [MaxLength(500)]
    public string? FacebookUrl { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public bool IsApprover { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public ICollection<EmployeeContact> EmergencyContacts { get; set; } = new List<EmployeeContact>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<Request> Requests { get; set; } = new List<Request>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<AssetAssignment> AssetAssignments { get; set; } = new List<AssetAssignment>();
    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public ICollection<TicketStar> StarredTickets { get; set; } = new List<TicketStar>();
    public ICollection<MeetingRoomBooking> MeetingRoomBookings { get; set; } = new List<MeetingRoomBooking>();
}
