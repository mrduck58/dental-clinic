using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class MaterialRequestRepository(AppDbContext db) : IMaterialRequestRepository
{
    public async Task AddAsync(MaterialRequest request, CancellationToken ct = default)
    {
        await db.MaterialRequests.AddAsync(request, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MaterialRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.MaterialRequests.Include(m => m.Items).FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<MaterialRequest>> SearchAsync(
        string? status,
        Guid? patientId,
        string? patientName,
        CancellationToken ct = default)
    {
        var q = db.MaterialRequests.Include(m => m.Items).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MaterialRequestStatus>(status, true, out var st))
            q = q.Where(m => m.Status == st);

        // Lọc theo bệnh nhân: khớp PatientId HOẶC tên (bao gồm dữ liệu cũ chưa có id).
        var hasId = patientId is Guid;
        var hasName = !string.IsNullOrWhiteSpace(patientName);
        if (hasId && hasName)
            q = q.Where(m => m.PatientId == patientId || m.PatientName == patientName);
        else if (hasId)
            q = q.Where(m => m.PatientId == patientId);
        else if (hasName)
            q = q.Where(m => m.PatientName == patientName);

        return await q
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(MaterialRequest request, CancellationToken ct = default)
    {
        db.MaterialRequests.Update(request);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IMaterialRequestTransaction?> BeginTransactionAsync(CancellationToken ct = default)
    {
        // InMemory provider (dùng trong unit test) không hỗ trợ transaction — chỉ bọc transaction thật khi
        // chạy trên provider quan hệ (Postgres).
        if (!db.Database.IsRelational()) return null;

        var tx = await db.Database.BeginTransactionAsync(ct);
        return new EfMaterialRequestTransaction(tx);
    }

    private sealed class EfMaterialRequestTransaction(IDbContextTransaction tx) : IMaterialRequestTransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => tx.CommitAsync(ct);
        public ValueTask DisposeAsync() => tx.DisposeAsync();
    }
}
