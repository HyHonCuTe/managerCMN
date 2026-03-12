using managerCMN.Models.Entities;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
    }

    public async Task UpdateAsync(Department department)
    {
        _unitOfWork.Departments.Update(department);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var department = await _unitOfWork.Departments.GetByIdAsync(id);
        if (department != null)
        {
            _unitOfWork.Departments.Remove(department);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
