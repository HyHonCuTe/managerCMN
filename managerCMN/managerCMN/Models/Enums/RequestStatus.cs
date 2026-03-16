namespace managerCMN.Models.Enums;

public enum RequestStatus
{
    Pending = 0,
    Approver1Approved = 1,
    Approver2Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    FullyApproved = 5
}
