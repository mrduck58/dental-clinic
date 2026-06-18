using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ILeaveRequestRepository
{
    Task<IEnumerable<LeaveRequest>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<LeaveRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(LeaveRequest leaveRequest, CancellationToken ct = default);
    Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken ct = default);
}
