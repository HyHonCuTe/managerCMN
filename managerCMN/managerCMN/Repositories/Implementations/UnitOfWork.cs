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
        LeaveBalances = new LeaveBalanceRepository(context);
        LeaveRequests = new LeaveRequestRepository(context);
        Requests = new RequestRepository(context);
        RequestApprovals = new RequestApprovalRepository(context);
        Attendances = new AttendanceRepository(context);
        Assets = new AssetRepository(context);
        AssetAssignments = new AssetAssignmentRepository(context);
        Tickets = new TicketRepository(context);
        Users = new UserRepository(context);
        SystemLogs = new SystemLogRepository(context);
        Notifications = new NotificationRepository(context);
    }

    public IEmployeeRepository Employees { get; }
    public IDepartmentRepository Departments { get; }
    public IContractRepository Contracts { get; }
    public ILeaveBalanceRepository LeaveBalances { get; }
    public ILeaveRequestRepository LeaveRequests { get; }
    public IRequestRepository Requests { get; }
    public IRequestApprovalRepository RequestApprovals { get; }
    public IAttendanceRepository Attendances { get; }
    public IAssetRepository Assets { get; }
    public IAssetAssignmentRepository AssetAssignments { get; }
    public ITicketRepository Tickets { get; }
    public IUserRepository Users { get; }
    public ISystemLogRepository SystemLogs { get; }
    public INotificationRepository Notifications { get; }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
