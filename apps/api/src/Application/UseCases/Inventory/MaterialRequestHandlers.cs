using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class MaterialRequestDto
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DentistName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? HandledAt { get; set; }
    public string? HandledBy { get; set; }
}

/// <summary>Danh sách yêu cầu vật tư từ bác sĩ (cho trang nhập–xuất vật tư của staff).</summary>
public class GetMaterialRequestsHandler(AppDbContext dbContext)
{
    public async Task<List<MaterialRequestDto>> HandleAsync(string? status, CancellationToken ct = default)
    {
        var query = dbContext.MaterialRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MaterialRequestStatus>(status, true, out var st))
            query = query.Where(m => m.Status == st);

        var rows = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(m => new MaterialRequestDto
        {
            Id = m.Id,
            CourseName = m.CourseName,
            PatientName = m.PatientName,
            DentistName = m.DentistName,
            Content = m.Content,
            Status = m.Status.ToString(),
            CreatedAt = m.CreatedAt,
            HandledAt = m.HandledAt,
            HandledBy = m.HandledBy
        }).ToList();
    }
}

/// <summary>Đánh dấu một yêu cầu vật tư đã được kho xử lý.</summary>
public class MarkMaterialRequestDoneHandler(AppDbContext dbContext)
{
    public async Task HandleAsync(Guid id, string handledBy, CancellationToken ct = default)
    {
        var request = await dbContext.MaterialRequests.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu vật tư.");

        request.MarkDone(handledBy);
        await dbContext.SaveChangesAsync(ct);
    }
}
