using System.ComponentModel.DataAnnotations;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class ProjectTemplateListViewModel
{
    public int ProjectTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int TaskCount { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class ProjectTemplateCreateViewModel
{
    [Required(ErrorMessage = "Tên template không được để trống")]
    [MaxLength(200, ErrorMessage = "Tên template không quá 200 ký tự")]
    [Display(Name = "Tên template")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;

    public List<ProjectTemplateTaskFormViewModel> Tasks { get; set; } = new();
}

public class ProjectTemplateEditViewModel : ProjectTemplateCreateViewModel
{
    public int ProjectTemplateId { get; set; }
}

public class ProjectTemplateTaskFormViewModel
{
    public int ProjectTemplateTaskId { get; set; }

    [Required(ErrorMessage = "Tên task không được để trống")]
    [MaxLength(300, ErrorMessage = "Tên task không quá 300 ký tự")]
    [Display(Name = "Tên task")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Độ ưu tiên")]
    public ProjectTaskPriority Priority { get; set; } = ProjectTaskPriority.Medium;

    [Display(Name = "Giờ ước tính")]
    public decimal? EstimatedHours { get; set; }

    public int SortOrder { get; set; }

    /// <summary>-1 = root task; >= 0 = index of parent in the flat Tasks list</summary>
    public int ParentIndex { get; set; } = -1;
}

public class ProjectTemplateDetailViewModel
{
    public int ProjectTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public List<ProjectTemplateTaskFormViewModel> Tasks { get; set; } = new();
}
