using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class RoomRepository(AppDbContext db) : IRoomRepository
{
    public async Task<IEnumerable<Room>> GetAllAsync(CancellationToken ct = default)
        => await db.Rooms
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.Code)
            .ToListAsync(ct);

    public async Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
        => await db.Rooms.AnyAsync(r =>
            r.Code == code.ToUpperInvariant() && (excludeId == null || r.Id != excludeId), ct);

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken ct = default)
        => await db.Rooms.AnyAsync(r =>
            r.Name.ToLower() == name.ToLower() && (excludeId == null || r.Id != excludeId), ct);

    public async Task AddAsync(Room room, CancellationToken ct = default)
    {
        await db.Rooms.AddAsync(room, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        db.Rooms.Update(room);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Room room, CancellationToken ct = default)
    {
        db.Rooms.Remove(room);
        await db.SaveChangesAsync(ct);
    }
}
