using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class MaterialRequestRepository(AppDbContext db) : IMaterialRequestRepository
{
    public async Task AddAsync(MaterialRequest request, CancellationToken ct = default)
    {
        await db.MaterialRequests.AddAsync(request, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MaterialRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.MaterialRequests.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<MaterialRequest>> SearchAsync(
        string? status,
        Guid? patientId,
        string? patientName,
        CancellationToken ct = default)
    {
        var q = db.MaterialRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MaterialRequestStatus>(status, true, out var st))
            q = q.Where(m => m.Status == st);

        // Lọc theo bệnh nhân: khớp PatientId (lưu ở CourseId) HOẶC tên (bao gồm dữ liệu cũ chưa có id).
        var hasId = patientId is Guid;
        var hasName = !string.IsNullOrWhiteSpace(patientName);
        if (hasId && hasName)
            q = q.Where(m => m.CourseId == patientId || m.PatientName == patientName);
        else if (hasId)
            q = q.Where(m => m.CourseId == patientId);
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
}
