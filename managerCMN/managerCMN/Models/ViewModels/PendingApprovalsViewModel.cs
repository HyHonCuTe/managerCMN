using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public class PendingApprovalsViewModel
{
    public IEnumerable<Request> Requests { get; set; } = new List<Request>();
    public RequestStatus? FilterStatus { get; set; }
    public RequestType? FilterType { get; set; }
    public DateTime? FilterDateFrom { get; set; }
    public DateTime? FilterDateTo { get; set; }
    public int CurrentEmployeeId { get; set; }
}
