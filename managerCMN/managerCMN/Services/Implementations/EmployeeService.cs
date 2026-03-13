using System.Text.RegularExpressions;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;

    public EmployeeService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
    }

    public async Task UpdateAsync(Employee employee)
    {
        _unitOfWork.Employees.Update(employee);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await _unitOfWork.Employees.GetByIdAsync(id);
        if (employee != null)
        {
            employee.Status = EmployeeStatus.Resigned;
            _unitOfWork.Employees.Update(employee);
            await _unitOfWork.SaveChangesAsync();
        }
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
