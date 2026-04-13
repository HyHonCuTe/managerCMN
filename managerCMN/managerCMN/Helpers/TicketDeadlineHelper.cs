using managerCMN.Models.Entities;
using managerCMN.Models.Enums;

namespace managerCMN.Helpers;

public static class TicketDeadlineHelper
{
    public static bool IsExpired(this Ticket ticket, DateTime? referenceDate = null)
    {
        if (ticket.Deadline is null || ticket.Status.IsTerminal())
            return false;

        var today = (referenceDate ?? DateTimeHelper.VietnamToday).Date;
        return NormalizeDeadlineDate(ticket.Deadline.Value) < today;
    }

    public static bool IsNearDeadline(this Ticket ticket, DateTime? referenceDate = null, int daysAhead = 2)
    {
        if (ticket.Deadline is null || ticket.Status.IsTerminal())
            return false;

        var today = (referenceDate ?? DateTimeHelper.VietnamToday).Date;
        var deadlineDate = NormalizeDeadlineDate(ticket.Deadline.Value);

        return deadlineDate >= today && deadlineDate <= today.AddDays(daysAhead);
    }

    public static DateTime? GetDeadlineDate(this Ticket ticket)
        => ticket.Deadline.HasValue
            ? NormalizeDeadlineDate(ticket.Deadline.Value)
            : null;

    public static bool IsTerminal(this TicketStatus status)
        => status is TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled;

    private static DateTime NormalizeDeadlineDate(DateTime deadline)
    {
        if (deadline.Kind is DateTimeKind.Utc or DateTimeKind.Local)
            return deadline.ToVietnamTime().Date;

        return deadline.Date;
    }
}
