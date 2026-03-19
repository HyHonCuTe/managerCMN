using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace managerCMN.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _env;

    public TicketService(IUnitOfWork unitOfWork, INotificationService notificationService, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _env = env;
    }

    #region Existing methods (backward compat)

    public async Task<IEnumerable<Ticket>> GetAllAsync()
        => await _unitOfWork.Tickets.GetAllAsync();

    public async Task<Ticket?> GetByIdAsync(int id)
        => await _unitOfWork.Tickets.GetByIdAsync(id);

    public async Task<IEnumerable<Ticket>> GetByStatusAsync(TicketStatus status)
        => await _unitOfWork.Tickets.GetByStatusAsync(status);

    public async Task<IEnumerable<Ticket>> GetByCreatorAsync(int employeeId)
        => await _unitOfWork.Tickets.GetByCreatorAsync(employeeId);

    public async Task<IEnumerable<Ticket>> GetByAssigneeAsync(int employeeId)
        => await _unitOfWork.Tickets.GetByAssigneeAsync(employeeId);

    public async Task CreateAsync(Ticket ticket)
    {
        ticket.Status = TicketStatus.Open;
        ticket.CreatedDate = DateTime.UtcNow;
        await _unitOfWork.Tickets.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AssignAsync(int ticketId, int assigneeId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
        if (ticket == null) return;

        ticket.AssignedTo = assigneeId;
        ticket.Status = TicketStatus.InProgress;
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResolveAsync(int ticketId, string resolution)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
        if (ticket == null) return;

        ticket.Status = TicketStatus.Resolved;
        ticket.Resolution = resolution;
        ticket.ResolvedDate = DateTime.UtcNow;
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CloseAsync(int ticketId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
        if (ticket == null) return;

        ticket.Status = TicketStatus.Closed;
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();
    }

    #endregion

    #region New email-like methods

    public async Task CreateWithRecipientsAsync(Ticket ticket, List<int> recipientIds, List<IFormFile>? attachments, int creatorId)
    {
        ticket.Status = TicketStatus.Open;
        ticket.CreatedDate = DateTime.UtcNow;
        ticket.CreatedBy = creatorId;

        // Add recipients
        foreach (var recipientId in recipientIds.Distinct())
        {
            ticket.Recipients.Add(new TicketRecipient
            {
                EmployeeId = recipientId,
                Status = TicketRecipientStatus.Pending,
                AddedDate = DateTime.UtcNow,
                AddedById = creatorId
            });
        }

        // Handle attachments
        if (attachments?.Count > 0)
        {
            await SaveAttachmentsAsync(ticket.Attachments, attachments, creatorId, "tickets");
        }

        await _unitOfWork.Tickets.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        // Notify recipients
        var creator = await _unitOfWork.Employees.GetByIdAsync(creatorId);
        var creatorName = creator?.FullName ?? "Ai đó";
        foreach (var recipientId in recipientIds.Distinct())
        {
            await NotifyEmployeeAsync(recipientId, "Ticket mới", $"{creatorName} đã gửi ticket cho bạn: {ticket.Title}");
        }
    }

    public async Task<Ticket?> GetTicketDetailAsync(int ticketId)
        => await _unitOfWork.Tickets.GetWithFullDetailsAsync(ticketId);

    public async Task<IEnumerable<Ticket>> GetReceivedTicketsAsync(int employeeId)
        => await _unitOfWork.Tickets.GetByRecipientAsync(employeeId);

    public async Task<IEnumerable<Ticket>> GetSentTicketsAsync(int employeeId)
        => await _unitOfWork.Tickets.GetSentByCreatorAsync(employeeId);

    public async Task<IEnumerable<Ticket>> GetAllTicketsAsync()
        => await _unitOfWork.Tickets.GetAllWithRecipientsAsync();

    public async Task ReplyAsync(int ticketId, int senderId, string content, List<IFormFile>? attachments)
    {
        var ticket = await _unitOfWork.Tickets.GetWithFullDetailsAsync(ticketId);
        if (ticket == null) return;

        var message = new TicketMessage
        {
            TicketId = ticketId,
            SenderId = senderId,
            Content = content,
            MessageType = TicketMessageType.Reply,
            CreatedDate = DateTime.UtcNow
        };

        // Handle message attachments
        if (attachments?.Count > 0)
        {
            await SaveAttachmentsAsync(message.Attachments, attachments, senderId, "tickets/messages");
        }

        await _unitOfWork.TicketMessages.AddAsync(message);

        // Update ticket status if needed
        if (ticket.Status == TicketStatus.Open || ticket.Status == TicketStatus.Reopened)
        {
            ticket.Status = TicketStatus.InProgress;
            _unitOfWork.Tickets.Update(ticket);
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify relevant parties
        var sender = await _unitOfWork.Employees.GetByIdAsync(senderId);
        var senderName = sender?.FullName ?? "Ai đó";

        // Notify creator if sender is not creator
        if (ticket.CreatedBy != senderId)
        {
            await NotifyEmployeeAsync(ticket.CreatedBy, "Phản hồi ticket", $"{senderName} đã phản hồi ticket: {ticket.Title}");
        }

        // Notify other recipients
        foreach (var recipient in ticket.Recipients.Where(r => r.EmployeeId != senderId))
        {
            await NotifyEmployeeAsync(recipient.EmployeeId, "Phản hồi ticket", $"{senderName} đã phản hồi ticket: {ticket.Title}");
        }
    }

    public async Task ForwardAsync(int ticketId, int forwarderId, List<int> recipientIds, string content, List<IFormFile>? attachments)
    {
        var ticket = await _unitOfWork.Tickets.GetWithFullDetailsAsync(ticketId);
        if (ticket == null) return;

        // Get existing recipient IDs
        var existingRecipientIds = ticket.Recipients.Select(r => r.EmployeeId).ToHashSet();
        var newRecipientIds = recipientIds.Where(id => !existingRecipientIds.Contains(id) && id != ticket.CreatedBy).ToList();

        // Add new recipients
        foreach (var recipientId in newRecipientIds)
        {
            var newRecipient = new TicketRecipient
            {
                TicketId = ticketId,
                EmployeeId = recipientId,
                Status = TicketRecipientStatus.Pending,
                AddedDate = DateTime.UtcNow,
                AddedById = forwarderId
            };
            await _unitOfWork.TicketRecipients.AddAsync(newRecipient);
        }

        // Create forward message
        var message = new TicketMessage
        {
            TicketId = ticketId,
            SenderId = forwarderId,
            Content = content,
            MessageType = TicketMessageType.Forward,
            CreatedDate = DateTime.UtcNow
        };

        // Handle attachments
        if (attachments?.Count > 0)
        {
            await SaveAttachmentsAsync(message.Attachments, attachments, forwarderId, "tickets/messages");
        }

        await _unitOfWork.TicketMessages.AddAsync(message);

        // Update forwarder's status
        var forwarderRecipient = ticket.Recipients.FirstOrDefault(r => r.EmployeeId == forwarderId);
        if (forwarderRecipient != null)
        {
            forwarderRecipient.Status = TicketRecipientStatus.Forwarded;
            _unitOfWork.TicketRecipients.Update(forwarderRecipient);
        }

        // Update ticket status
        if (ticket.Status != TicketStatus.Closed && ticket.Status != TicketStatus.Cancelled)
        {
            ticket.Status = TicketStatus.Forwarded;
            _unitOfWork.Tickets.Update(ticket);
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify new recipients
        var forwarder = await _unitOfWork.Employees.GetByIdAsync(forwarderId);
        var forwarderName = forwarder?.FullName ?? "Ai đó";
        foreach (var recipientId in newRecipientIds)
        {
            await NotifyEmployeeAsync(recipientId, "Ticket được chuyển tiếp", $"{forwarderName} đã chuyển tiếp ticket cho bạn: {ticket.Title}");
        }

        // Notify creator
        if (ticket.CreatedBy != forwarderId)
        {
            await NotifyEmployeeAsync(ticket.CreatedBy, "Ticket đã chuyển tiếp", $"{forwarderName} đã chuyển tiếp ticket: {ticket.Title}");
        }
    }

    public async Task UpdateRecipientStatusAsync(int ticketRecipientId, TicketRecipientStatus status)
    {
        var recipient = await _unitOfWork.TicketRecipients.GetByIdAsync(ticketRecipientId);
        if (recipient == null) return;

        recipient.Status = status;
        if (status == TicketRecipientStatus.Completed)
        {
            recipient.CompletedDate = DateTime.UtcNow;
        }

        _unitOfWork.TicketRecipients.Update(recipient);
        await _unitOfWork.SaveChangesAsync();

        // Notify creator about status change
        var ticket = await _unitOfWork.Tickets.GetWithFullDetailsAsync(recipient.TicketId);
        if (ticket != null)
        {
            var employee = await _unitOfWork.Employees.GetByIdAsync(recipient.EmployeeId);
            var employeeName = employee?.FullName ?? "Ai đó";
            var statusText = status switch
            {
                TicketRecipientStatus.InProgress => "đang xử lý",
                TicketRecipientStatus.Completed => "đã hoàn thành",
                _ => "đã cập nhật trạng thái"
            };
            await NotifyEmployeeAsync(ticket.CreatedBy, "Cập nhật ticket", $"{employeeName} {statusText} ticket: {ticket.Title}");
        }
    }

    public async Task MarkAsReadAsync(int ticketId, int employeeId)
    {
        var recipient = await _unitOfWork.TicketRecipients.GetByTicketAndEmployeeAsync(ticketId, employeeId);
        if (recipient != null && recipient.ReadDate == null)
        {
            recipient.ReadDate = DateTime.UtcNow;
            if (recipient.Status == TicketRecipientStatus.Pending)
            {
                recipient.Status = TicketRecipientStatus.Read;
            }
            _unitOfWork.TicketRecipients.Update(recipient);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Employee>> GetAvailableRecipientsAsync(int? excludeEmployeeId = null)
    {
        var employees = (await _unitOfWork.Employees.GetAllAsync())
            .Where(e => e.Status == EmployeeStatus.Active);
        if (excludeEmployeeId.HasValue)
        {
            employees = employees.Where(e => e.EmployeeId != excludeEmployeeId.Value);
        }
        return employees.OrderBy(e => e.FullName);
    }

    public async Task<TicketAttachment?> GetAttachmentAsync(int attachmentId)
        => await _unitOfWork.TicketAttachments.GetByIdAsync(attachmentId);

    #endregion

    #region Helper methods

    private async Task SaveAttachmentsAsync(ICollection<TicketAttachment> attachmentCollection, List<IFormFile> files, int uploadedById, string subFolder)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", subFolder);
        Directory.CreateDirectory(uploadsDir);

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            attachmentCollection.Add(new TicketAttachment
            {
                FileName = file.FileName,
                FilePath = $"/uploads/{subFolder}/{fileName}",
                FileSize = file.Length,
                ContentType = file.ContentType,
                UploadedById = uploadedById,
                UploadedDate = DateTime.UtcNow
            });
        }
    }

    private async Task NotifyEmployeeAsync(int employeeId, string title, string message)
    {
        var user = (await _unitOfWork.Users.FindAsync(u => u.EmployeeId == employeeId)).FirstOrDefault();
        if (user != null)
        {
            await _notificationService.CreateAsync(user.UserId, title, message);
        }
    }

    #endregion
}
