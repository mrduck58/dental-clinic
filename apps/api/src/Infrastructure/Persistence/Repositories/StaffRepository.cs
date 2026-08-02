using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class StaffRepository(AppDbContext db) : IStaffRepository
{
    public async Task<Staff?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Staffs.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Staff?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await db.Staffs.FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task AddAsync(Staff staff, CancellationToken ct = default)
    {
        await db.Staffs.AddAsync(staff, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Staff staff, CancellationToken ct = default)
    {
        db.Staffs.Update(staff);
        await db.SaveChangesAsync(ct);
    }
}
