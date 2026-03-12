using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalEmployees { get; set; }
    public int PendingRequests { get; set; }
    public int ActiveTickets { get; set; }
    public int TotalAssets { get; set; }
    public IEnumerable<Contract> ExpiringContracts { get; set; } = [];
}
