namespace managerCMN.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IEmployeeRepository Employees { get; }
    IDepartmentRepository Departments { get; }
    IContractRepository Contracts { get; }
    IMeetingRoomRepository MeetingRooms { get; }
    IMeetingRoomBookingRepository MeetingRoomBookings { get; }
    ILeaveBalanceRepository LeaveBalances { get; }
    ILeaveRequestRepository LeaveRequests { get; }
    IRequestRepository Requests { get; }
    IRequestApprovalRepository RequestApprovals { get; }
    IAttendanceRepository Attendances { get; }
    IPunchRecordRepository PunchRecords { get; }
    IHolidayRepository Holidays { get; }
    IFullAttendanceEmployeeRepository FullAttendanceEmployees { get; }
    IAssetRepository Assets { get; }
    IAssetAssignmentRepository AssetAssignments { get; }
    ITicketRepository Tickets { get; }
    ITicketRecipientRepository TicketRecipients { get; }
    ITicketMessageRepository TicketMessages { get; }
    ITicketAttachmentRepository TicketAttachments { get; }
    IUserRepository Users { get; }
    ISystemLogRepository SystemLogs { get; }
    INotificationRepository Notifications { get; }
    IPostHistoryRepository PostHistories { get; }
    IPermissionRepository Permissions { get; }
    Task<int> SaveChangesAsync();
}
