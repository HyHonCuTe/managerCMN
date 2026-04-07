using System.Security.Claims;
using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace managerCMN.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IWebHostEnvironment _env;
    private readonly ISystemLogService _logService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TicketService(IUnitOfWork unitOfWork, INotificationService notificationService, IWebHostEnvironment env, ISystemLogService logService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _env = env;
        _logService = logService;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }

    private int? GetCurrentEmployeeId()
    {
        var employeeIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("EmployeeId")?.Value;
        return int.TryParse(employeeIdClaim, out var id) ? id : null;
    }

    private bool IsCurrentUserAdmin()
        => _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;

    private string? GetClientIP() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

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

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo mới Ticket",
            "Ticket",
            null,
            new { ticket.TicketId, ticket.Title, ticket.Priority, ticket.CreatedBy },
            GetClientIP()
        );
    }

    public async Task UpdateAsync(Ticket ticket)
    {
        var existing = await _unitOfWork.Tickets.GetByIdAsync(ticket.TicketId);
        var dataBefore = existing != null ? new { existing.TicketId, existing.Title, existing.Status, existing.Priority, existing.AssignedTo } : null;

        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Cập nhật Ticket",
            "Ticket",
            dataBefore,
            new { ticket.TicketId, ticket.Title, ticket.Status, ticket.Priority, ticket.AssignedTo },
            GetClientIP()
        );
    }

    public async Task AssignAsync(int ticketId, int assigneeId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
        if (ticket == null) return;

        var dataBefore = new { ticket.TicketId, ticket.Title, ticket.Status, ticket.AssignedTo };

        ticket.AssignedTo = assigneeId;
        ticket.Status = TicketStatus.InProgress;
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Gán Ticket",
            "Ticket",
            dataBefore,
            new { ticket.TicketId, ticket.Title, ticket.Status, ticket.AssignedTo },
            GetClientIP()
        );
    }

    public async Task ResolveAsync(int ticketId, string resolution)
    {
        var ticket = await _unitOfWork.Tickets.GetWithFullDetailsAsync(ticketId);
        if (ticket == null)
            throw new KeyNotFoundException($"Ticket {ticketId} was not found.");

        var currentEmployeeId = GetCurrentEmployeeId();
        if (!currentEmployeeId.HasValue || !CanResolveTicket(ticket, currentEmployeeId.Value, IsCurrentUserAdmin()))
            throw new UnauthorizedAccessException("Current employee cannot resolve this ticket.");

        var dataBefore = new { ticket.TicketId, ticket.Title, ticket.Status };

        ticket.Status = TicketStatus.Resolved;
        ticket.Resolution = resolution;
        ticket.ResolvedDate = DateTime.UtcNow;
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Giải quyết Ticket",
            "Ticket",
            dataBefore,
            new { ticket.TicketId, ticket.Title, ticket.Status, ticket.Resolution },
            GetClientIP()
        );
    }

    public async Task CloseAsync(int ticketId)
    {
        var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
        if (ticket == null) return;

        var dataBefore = new { ticket.TicketId, ticket.Title, ticket.Status };

        ticket.Status = TicketStatus.Closed;
        _unitOfWork.Tickets.Update(ticket);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Đóng Ticket",
            "Ticket",
            dataBefore,
            new { ticket.TicketId, ticket.Title, ticket.Status },
            GetClientIP()
        );
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

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Tạo Ticket với người nhận",
            "Ticket",
            null,
            new { ticket.TicketId, ticket.Title, ticket.Priority, RecipientCount = recipientIds.Count },
            GetClientIP()
        );

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
        if (ticket == null)
            throw new KeyNotFoundException($"Ticket {ticketId} was not found.");

        var currentEmployeeId = GetCurrentEmployeeId();
        if (!currentEmployeeId.HasValue
            || currentEmployeeId.Value != senderId
            || !CanReplyToTicket(ticket, currentEmployeeId.Value, IsCurrentUserAdmin()))
        {
            throw new UnauthorizedAccessException("Current employee cannot reply to this ticket.");
        }

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

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Phản hồi Ticket",
            "TicketMessage",
            null,
            new { message.TicketMessageId, message.TicketId, message.SenderId },
            GetClientIP()
        );

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
        if (ticket == null)
            throw new KeyNotFoundException($"Ticket {ticketId} was not found.");

        var currentEmployeeId = GetCurrentEmployeeId();
        if (!currentEmployeeId.HasValue
            || currentEmployeeId.Value != forwarderId
            || !CanForwardTicket(ticket, currentEmployeeId.Value, IsCurrentUserAdmin()))
        {
            throw new UnauthorizedAccessException("Current employee cannot forward this ticket.");
        }

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

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Chuyển tiếp Ticket",
            "Ticket",
            null,
            new { ticketId, ForwarderId = forwarderId, NewRecipientCount = newRecipientIds.Count },
            GetClientIP()
        );

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

        var dataBefore = new { recipient.TicketRecipientId, recipient.TicketId, recipient.EmployeeId, recipient.Status };

        recipient.Status = status;
        if (status == TicketRecipientStatus.Completed)
        {
            recipient.CompletedDate = DateTime.UtcNow;
        }

        _unitOfWork.TicketRecipients.Update(recipient);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Cập nhật trạng thái người nhận Ticket",
            "TicketRecipient",
            dataBefore,
            new { recipient.TicketRecipientId, recipient.TicketId, recipient.EmployeeId, recipient.Status },
            GetClientIP()
        );

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

    public async Task<HashSet<int>> GetStarredTicketIdsAsync(int employeeId)
        => await _unitOfWork.TicketStars.GetStarredTicketIdsAsync(employeeId);

    public async Task<bool> ToggleStarAsync(int ticketId, int employeeId, bool isAdmin = false)
    {
        var ticket = await _unitOfWork.Tickets.Query()
            .Include(t => t.Recipients)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId);

        if (ticket == null)
            throw new KeyNotFoundException($"Ticket {ticketId} was not found.");

        var hasAccess = isAdmin
            || ticket.CreatedBy == employeeId
            || ticket.Recipients.Any(r => r.EmployeeId == employeeId);

        if (!hasAccess)
            throw new UnauthorizedAccessException("Current employee cannot star this ticket.");

        var existingStar = await _unitOfWork.TicketStars.GetByTicketAndEmployeeAsync(ticketId, employeeId);
        if (existingStar != null)
        {
            _unitOfWork.TicketStars.Remove(existingStar);
            await _unitOfWork.SaveChangesAsync();

            await _logService.LogAsync(
                GetCurrentUserId(),
                "Bo danh dau sao Ticket",
                "TicketStar",
                new { existingStar.TicketStarId, existingStar.TicketId, existingStar.EmployeeId },
                null,
                GetClientIP()
            );

            return false;
        }

        var newStar = new TicketStar
        {
            TicketId = ticketId,
            EmployeeId = employeeId,
            StarredAt = DateTime.UtcNow
        };

        await _unitOfWork.TicketStars.AddAsync(newStar);
        await _unitOfWork.SaveChangesAsync();

        await _logService.LogAsync(
            GetCurrentUserId(),
            "Danh dau sao Ticket",
            "TicketStar",
            null,
            new { newStar.TicketStarId, newStar.TicketId, newStar.EmployeeId },
            GetClientIP()
        );

        return true;
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
        => await _unitOfWork.TicketAttachments.Query()
            .Include(attachment => attachment.TicketMessage)
            .FirstOrDefaultAsync(attachment => attachment.TicketAttachmentId == attachmentId);

    #endregion

    #region Helper methods

    private async Task SaveAttachmentsAsync(ICollection<TicketAttachment> attachmentCollection, List<IFormFile> files, int uploadedById, string subFolder)
    {
        try
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
        catch (Exception)
        {
            // Log error but don't stop the ticket creation/reply
            // The ticket will be created without attachments
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

    private static bool CanReplyToTicket(Ticket ticket, int employeeId, bool isAdmin)
        => isAdmin
            || ticket.CreatedBy == employeeId
            || ticket.Recipients.Any(recipient => recipient.EmployeeId == employeeId);

    private static bool CanForwardTicket(Ticket ticket, int employeeId, bool isAdmin)
        => isAdmin
            || ticket.Recipients.Any(recipient => recipient.EmployeeId == employeeId);

    private static bool CanResolveTicket(Ticket ticket, int employeeId, bool isAdmin)
        => CanReplyToTicket(ticket, employeeId, isAdmin);

    #endregion
}
