using managerCMN.Models.Entities;
using managerCMN.Models.Enums;
using managerCMN.Repositories.Interfaces;
using managerCMN.Services.Interfaces;

namespace managerCMN.Services.Implementations;

public class RequestService : IRequestService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILeaveService _leaveService;

    public RequestService(IUnitOfWork unitOfWork, ILeaveService leaveService)
    {
        _unitOfWork = unitOfWork;
        _leaveService = leaveService;
    }

    public async Task<IEnumerable<Request>> GetAllAsync()
        => await _unitOfWork.Requests.GetAllAsync();

    public async Task<Request?> GetByIdAsync(int id)
        => await _unitOfWork.Requests.GetByIdAsync(id);

    public async Task<Request?> GetWithAttachmentsAsync(int id)
        => await _unitOfWork.Requests.GetWithAttachmentsAsync(id);

    public async Task<IEnumerable<Request>> GetByEmployeeAsync(int employeeId)
        => await _unitOfWork.Requests.GetByEmployeeAsync(employeeId);

    public async Task<IEnumerable<Request>> GetByStatusAsync(RequestStatus status)
        => await _unitOfWork.Requests.GetByStatusAsync(status);

    public async Task CreateAsync(Request request)
    {
        request.Status = RequestStatus.Pending;
        request.CreatedDate = DateTime.UtcNow;
        await _unitOfWork.Requests.AddAsync(request);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ManagerApproveAsync(int requestId, int approverId)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null || request.Status != RequestStatus.Pending) return;

        request.Status = RequestStatus.ManagerApproved;
        request.ApproverId = approverId;
        request.ApprovedDate = DateTime.UtcNow;

        _unitOfWork.Requests.Update(request);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task HRApproveAsync(int requestId, int approverId)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null || request.Status != RequestStatus.ManagerApproved) return;

        request.Status = RequestStatus.HRApproved;
        request.ApproverId = approverId;
        request.ApprovedDate = DateTime.UtcNow;

        _unitOfWork.Requests.Update(request);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RejectAsync(int requestId, int approverId)
    {
        var request = await _unitOfWork.Requests.GetByIdAsync(requestId);
        if (request == null) return;

        request.Status = RequestStatus.Rejected;
        request.ApproverId = approverId;
        request.ApprovedDate = DateTime.UtcNow;

        _unitOfWork.Requests.Update(request);
        await _unitOfWork.SaveChangesAsync();
    }
}
