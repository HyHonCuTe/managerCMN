using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class RequestDetailViewModel
{
    public Request Request { get; set; } = null!;
    public bool CanApprove { get; set; }
    public int? CurrentApproverOrder { get; set; }
    public bool IsOwner { get; set; }
}
