using managerCMN.Models.ViewModels;

namespace managerCMN.Services.Interfaces;

public interface IProjectTemplateService
{
    Task<IEnumerable<ProjectTemplateListViewModel>> GetAllAsync();
    Task<IEnumerable<ProjectTemplateListViewModel>> GetAllActiveAsync();
    Task<ProjectTemplateDetailViewModel?> GetByIdAsync(int templateId);
    Task<int> CreateAsync(ProjectTemplateCreateViewModel vm, int creatorEmployeeId);
    Task UpdateAsync(ProjectTemplateEditViewModel vm);
    Task DeleteAsync(int templateId);
}
