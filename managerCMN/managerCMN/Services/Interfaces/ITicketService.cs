using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace managerCMN.Services.Interfaces;

public interface ITicketService
{
    // Existing methods (backward compat)
    Task<IEnumerable<Ticket>> GetAllAsync();
    Task<Ticket?> GetByIdAsync(int id);
    Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status);
    Task<IEnumerable<Ticket>> GetByCreatorAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetByAssigneeAsync(int employeeId);
    Task CreateAsync(Ticket ticket);
    Task UpdateAsync(Ticket ticket);
    Task AssignAsync(int ticketId, int assigneeId);
    Task ResolveAsync(int ticketId, string resolution);
    Task CloseAsync(int ticketId);

    // New email-like methods
    Task CreateWithRecipientsAsync(Ticket ticket, List<int> recipientIds, List<IFormFile>? attachments, int creatorId);
    Task<Ticket?> GetTicketDetailAsync(int ticketId);
    Task<IEnumerable<Ticket>> GetReceivedTicketsAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetSentTicketsAsync(int employeeId);
    Task<IEnumerable<Ticket>> GetAllTicketsAsync();

    Task ReplyAsync(int ticketId, int senderId, string content, List<IFormFile>? attachments);
    Task ForwardAsync(int ticketId, int forwarderId, List<int> recipientIds, string content, List<IFormFile>? attachments);

    Task UpdateRecipientStatusAsync(int ticketRecipientId, TicketRecipientStatus status);
    Task MarkAsReadAsync(int ticketId, int employeeId);

    Task<IEnumerable<Employee>> GetAvailableRecipientsAsync(int? excludeEmployeeId = null);
    Task<TicketAttachment?> GetAttachmentAsync(int attachmentId);
}
