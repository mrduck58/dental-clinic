using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class LeaveRequestRepository(AppDbContext db) : ILeaveRequestRepository
{
    public async Task<IEnumerable<LeaveRequest>> GetAllAsync(CancellationToken ct = default)
        => await db.LeaveRequests
            .Include(l => l.User).ThenInclude(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<LeaveRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.LeaveRequests
            .Include(l => l.User).ThenInclude(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.LeaveRequests
            .Include(l => l.User).ThenInclude(u => u.Employee).ThenInclude(e => e!.DentistProfile)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken ct = default)
    {
        await db.LeaveRequests.AddAsync(leaveRequest, ct);
        await db.SaveChangesAsync(ct);
        // Load User navigation property so ToDto can access user info
        await db.Entry(leaveRequest).Reference(l => l.User).LoadAsync(ct);
        if (leaveRequest.User != null)
        {
            await db.Entry(leaveRequest.User).Reference(u => u.Employee).LoadAsync(ct);
            if (leaveRequest.User.Employee != null)
            {
                await db.Entry(leaveRequest.User.Employee).Reference(e => e.DentistProfile).LoadAsync(ct);
            }
        }
    }

    public async Task UpdateAsync(LeaveRequest leaveRequest, CancellationToken ct = default)
    {
        db.LeaveRequests.Update(leaveRequest);
        await db.SaveChangesAsync(ct);
    }
}
