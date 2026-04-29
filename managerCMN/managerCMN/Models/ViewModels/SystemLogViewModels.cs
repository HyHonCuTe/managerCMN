using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class SystemLogIndexViewModel
{
    public IReadOnlyList<SystemLogListItemViewModel> Logs { get; set; } = [];
    public string? Module { get; set; }
    public string? LogAction { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<string> ModuleOptions { get; set; } = [];
    public IReadOnlyList<string> ActionOptions { get; set; } = [];
}

public class SystemLogListItemViewModel
{
    public SystemLog Log { get; set; } = null!;
    public string UserDisplayName { get; set; } = "Hệ thống";
    public string DetailPreview { get; set; } = "Không có dữ liệu chi tiết";
    public string DataBeforePretty { get; set; } = "Không có dữ liệu";
    public string DataAfterPretty { get; set; } = "Không có dữ liệu";
    public bool HasBeforeData { get; set; }
    public bool HasAfterData { get; set; }
}
