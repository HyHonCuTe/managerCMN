using managerCMN.Data;
using managerCMN.Repositories.Interfaces;

namespace managerCMN.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Employees = new EmployeeRepository(context);
        Departments = new DepartmentRepository(context);
        Contracts = new ContractRepository(context);
        MeetingRooms = new MeetingRoomRepository(context);
        MeetingRoomBookings = new MeetingRoomBookingRepository(context);
        LeaveBalances = new LeaveBalanceRepository(context);
        LeaveRequests = new LeaveRequestRepository(context);
        Requests = new RequestRepository(context);
        RequestApprovals = new RequestApprovalRepository(context);
        Attendances = new AttendanceRepository(context);
        PunchRecords = new PunchRecordRepository(context);
        Holidays = new HolidayRepository(context);
        FullAttendanceEmployees = new FullAttendanceEmployeeRepository(context);
        Assets = new AssetRepository(context);
        AssetAssignments = new AssetAssignmentRepository(context);
        Tickets = new TicketRepository(context);
        TicketRecipients = new TicketRecipientRepository(context);
        TicketMessages = new TicketMessageRepository(context);
        TicketAttachments = new TicketAttachmentRepository(context);
        TicketStars = new TicketStarRepository(context);
        Users = new UserRepository(context);
        SystemLogs = new SystemLogRepository(context);
        Notifications = new NotificationRepository(context);
        PostHistories = new PostHistoryRepository(context);
        Permissions = new PermissionRepository(context);
        Projects = new ProjectRepository(context);
        ProjectTasks = new ProjectTaskRepository(context);
        ProjectMembers = new ProjectMemberRepository(context);
    }

    public IEmployeeRepository Employees { get; }
    public IDepartmentRepository Departments { get; }
    public IContractRepository Contracts { get; }
    public IMeetingRoomRepository MeetingRooms { get; }
    public IMeetingRoomBookingRepository MeetingRoomBookings { get; }
    public ILeaveBalanceRepository LeaveBalances { get; }
    public ILeaveRequestRepository LeaveRequests { get; }
    public IRequestRepository Requests { get; }
    public IRequestApprovalRepository RequestApprovals { get; }
    public IAttendanceRepository Attendances { get; }
    public IPunchRecordRepository PunchRecords { get; }
    public IHolidayRepository Holidays { get; }
    public IFullAttendanceEmployeeRepository FullAttendanceEmployees { get; }
    public IAssetRepository Assets { get; }
    public IAssetAssignmentRepository AssetAssignments { get; }
    public ITicketRepository Tickets { get; }
    public ITicketRecipientRepository TicketRecipients { get; }
    public ITicketMessageRepository TicketMessages { get; }
    public ITicketAttachmentRepository TicketAttachments { get; }
    public ITicketStarRepository TicketStars { get; }
    public IUserRepository Users { get; }
    public ISystemLogRepository SystemLogs { get; }
    public INotificationRepository Notifications { get; }
    public IPostHistoryRepository PostHistories { get; }
    public IPermissionRepository Permissions { get; }
    public IProjectRepository Projects { get; }
    public IProjectTaskRepository ProjectTasks { get; }
    public IProjectMemberRepository ProjectMembers { get; }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
