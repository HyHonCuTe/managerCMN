using System.Security.Claims;
using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Implementations;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DepartmentService(IUnitOfWork unitOfWork, ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
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

    public async Task<IEnumerable<Department>> GetAllAsync()
        => await _unitOfWork.Departments.GetAllAsync();

    public async Task<Department?> GetByIdAsync(int id)
        => await _unitOfWork.Departments.GetByIdAsync(id);

    public async Task<Department?> GetWithEmployeesAsync(int id)
        => await _unitOfWork.Departments.GetWithEmployeesAsync(id);

    public async Task CreateAsync(Department department)
    {
        await _unitOfWork.Departments.AddAsync(department);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo mới Phòng ban",
            "Department",
            null,
            new { department.DepartmentId, department.DepartmentName, department.ManagerId },
            GetClientIP()
        );
    }

    public async Task UpdateAsync(Department department)
    {
        var existing = await _unitOfWork.Departments.GetByIdAsync(department.DepartmentId);
        var dataBefore = existing != null ? new { existing.DepartmentId, existing.DepartmentName, existing.ManagerId } : null;

        _unitOfWork.Departments.Update(department);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Cập nhật Phòng ban",
            "Department",
            dataBefore,
            new { department.DepartmentId, department.DepartmentName, department.ManagerId },
            GetClientIP()
        );
    }

    public async Task DeleteAsync(int id)
    {
        var department = await _unitOfWork.Departments.GetByIdAsync(id);
        if (department != null)
        {
            var dataBefore = new { department.DepartmentId, department.DepartmentName, department.ManagerId };

            _unitOfWork.Departments.Remove(department);
            await _unitOfWork.SaveChangesAsync();

            await _logService.LogAsync(
                GetCurrentUserId(),
                "Xóa Phòng ban",
                "Department",
                dataBefore,
                null,
                GetClientIP()
            );
        }
    }
}
