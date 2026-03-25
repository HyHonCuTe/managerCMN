using System.Security.Claims;
using System.Text.RegularExpressions;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmployeeService(IUnitOfWork unitOfWork, ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private string? GetClientIP() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public async Task<IEnumerable<Employee>> GetAllAsync()
        => await _unitOfWork.Employees.GetAllAsync();

    public async Task<Employee?> GetByIdAsync(int id)
        => await _unitOfWork.Employees.GetByIdAsync(id);

    public async Task<Employee?> GetWithDetailsAsync(int id)
        => await _unitOfWork.Employees.GetWithDetailsAsync(id);

    public async Task<IEnumerable<Employee>> GetByDepartmentAsync(int departmentId)
        => await _unitOfWork.Employees.GetByDepartmentAsync(departmentId);

    public async Task CreateAsync(Employee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.EmployeeCode))
            employee.EmployeeCode = await GenerateEmployeeCodeAsync();
        await _unitOfWork.Employees.AddAsync(employee);
        await _unitOfWork.SaveChangesAsync();

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo mới Nhân viên",
            "Employee",
            null,
            new { employee.EmployeeId, employee.EmployeeCode, employee.FullName, employee.Email, employee.DepartmentId },
            GetClientIP()
        );
    }

    public async Task UpdateAsync(Employee employee)
    {
        // Lấy dữ liệu trước khi update
        var existing = await _unitOfWork.Employees.GetByIdAsync(employee.EmployeeId);
        var dataBefore = existing != null ? new { existing.EmployeeId, existing.EmployeeCode, existing.FullName, existing.Email, existing.DepartmentId, existing.Status } : null;

        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync();

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Cập nhật Nhân viên",
            "Employee",
            dataBefore,
            new { employee.EmployeeId, employee.EmployeeCode, employee.FullName, employee.Email, employee.DepartmentId, employee.Status },
            GetClientIP()
        );
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await _unitOfWork.Employees.GetWithDetailsAsync(id);
        if (employee == null) return;

        // Lưu dữ liệu trước khi xóa
        var dataBefore = new { employee.EmployeeId, employee.EmployeeCode, employee.FullName, employee.Email, employee.DepartmentId };

        // Remove related data to avoid foreign key constraint errors
        // Remove LeaveBalances
        var leaveBalances = await _unitOfWork.LeaveBalances.FindAsync(lb => lb.EmployeeId == id);
        foreach (var lb in leaveBalances)
            _unitOfWork.LeaveBalances.Remove(lb);

        // Remove LeaveRequests
        var leaveRequests = await _unitOfWork.LeaveRequests.FindAsync(lr => lr.EmployeeId == id);
        foreach (var lr in leaveRequests)
            _unitOfWork.LeaveRequests.Remove(lr);

        // Remove Requests and RequestApprovals
        var requests = await _unitOfWork.Requests.FindAsync(r => r.EmployeeId == id);
        foreach (var r in requests)
        {
            var approvals = await _unitOfWork.RequestApprovals.FindAsync(ra => ra.RequestId == r.RequestId);
            foreach (var ra in approvals)
                _unitOfWork.RequestApprovals.Remove(ra);
            _unitOfWork.Requests.Remove(r);
        }

        // Remove Attendances
        var attendances = await _unitOfWork.Attendances.FindAsync(a => a.EmployeeId == id);
        foreach (var a in attendances)
            _unitOfWork.Attendances.Remove(a);

        // Remove AssetAssignments
        var assetAssignments = await _unitOfWork.AssetAssignments.FindAsync(aa => aa.EmployeeId == id);
        foreach (var aa in assetAssignments)
            _unitOfWork.AssetAssignments.Remove(aa);

        // Remove Contracts
        foreach (var contract in employee.Contracts.ToList())
            _unitOfWork.Contracts.Remove(contract);

        // Remove EmergencyContacts (handled by cascade or manually)
        // EmergencyContacts are part of employee navigation, will be deleted with employee

        // Finally remove the employee
        _unitOfWork.Employees.Remove(employee);
        await _unitOfWork.SaveChangesAsync();

        // Audit log
        await _logService.LogAsync(
            GetCurrentUserId(),
            "Xóa Nhân viên",
            "Employee",
            dataBefore,
            null,
            GetClientIP()
        );
    }

    public async Task<string> GenerateEmployeeCodeAsync()
    {
        var allEmployees = await _unitOfWork.Employees.GetAllAsync();
        int maxNumber = 0;
        var regex = new Regex(@"^A(\d{5})$", RegexOptions.IgnoreCase);
        foreach (var emp in allEmployees)
        {
            var match = regex.Match(emp.EmployeeCode ?? "");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int num) && num > maxNumber)
                maxNumber = num;
        }
        return $"A{(maxNumber + 1):D5}";
    }
}
