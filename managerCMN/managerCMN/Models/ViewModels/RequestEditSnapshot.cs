using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Models.ViewModels;

public sealed record RequestEditSnapshot(
    RequestType RequestType,
    LeaveReason? LeaveReason,
    DateTime StartTime,
    DateTime EndTime,
    decimal TotalDays,
    bool CountsAsWork,
    DateTime CreatedDate)
{
    public static RequestEditSnapshot From(Request request) => new(
        request.RequestType,
        request.LeaveReason,
        request.StartTime,
        request.EndTime,
        request.TotalDays,
        request.CountsAsWork,
        request.CreatedDate);
}
