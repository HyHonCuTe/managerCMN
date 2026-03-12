using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class TicketService : ITicketService
{
    private readonly IUnitOfWork _unitOfWork;

    public TicketService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
}
