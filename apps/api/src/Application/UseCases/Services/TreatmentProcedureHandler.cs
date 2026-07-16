using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Services;

public record ProcedureStepRequest(int StepNumber, string Name);

public class TreatmentProcedureDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TreatmentProcedureHandler(AppDbContext dbContext)
{
    public async Task<List<TreatmentProcedureDto>> GetByServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await dbContext.TreatmentProcedures
            .AsNoTracking()
            .Where(p => p.ServiceId == serviceId)
            .OrderBy(p => p.StepNumber)
            .Select(p => new TreatmentProcedureDto
            {
                Id = p.Id,
                ServiceId = p.ServiceId,
                StepNumber = p.StepNumber,
                Name = p.Name
            })
            .ToListAsync(ct);
    }

    /// <summary>Thay toàn bộ quy trình điều trị của một dịch vụ (xóa hết bước cũ, thêm bước mới).</summary>
    public async Task<List<TreatmentProcedureDto>> ReplaceForServiceAsync(
        Guid serviceId, List<ProcedureStepRequest> steps, CancellationToken ct = default)
    {
        var serviceExists = await dbContext.Services.AnyAsync(s => s.Id == serviceId, ct);
        if (!serviceExists)
            throw new NotFoundException("Không tìm thấy dịch vụ.");

        if (steps.Any(s => string.IsNullOrWhiteSpace(s.Name)))
            throw new ValidationException("Tên bước điều trị không được để trống.");

        if (steps.Select(s => s.StepNumber).Distinct().Count() != steps.Count)
            throw new ValidationException("Số thứ tự các bước không được trùng nhau.");

        var existing = await dbContext.TreatmentProcedures
            .Where(p => p.ServiceId == serviceId)
            .ToListAsync(ct);
        dbContext.TreatmentProcedures.RemoveRange(existing);

        foreach (var step in steps.OrderBy(s => s.StepNumber))
            dbContext.TreatmentProcedures.Add(TreatmentProcedure.Create(serviceId, step.StepNumber, step.Name.Trim()));

        await dbContext.SaveChangesAsync(ct);

        return await GetByServiceAsync(serviceId, ct);
    }
}
