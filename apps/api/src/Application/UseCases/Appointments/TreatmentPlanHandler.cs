using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateTreatmentPlanRequest(
    Guid AppointmentId,
    string Description,
    decimal? EstimatedCost);

public record UpdateTreatmentPlanRequest(
    Guid TreatmentPlanId,
    string Description,
    decimal? EstimatedCost);

public class TreatmentPlanHandler(AppDbContext dbContext)
{
    public async Task<TreatmentPlanDto> CreateAsync(CreateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment == null)
            throw new KeyNotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status != AppointmentStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể thêm liệu trình khi cuộc hẹn đang trong trạng thái đang khám.");

        var treatmentPlan = TreatmentPlan.Create(
            request.AppointmentId,
            request.Description,
            request.EstimatedCost);

        dbContext.TreatmentPlans.Add(treatmentPlan);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(treatmentPlan);
    }

    public async Task<TreatmentPlanDto> UpdateAsync(UpdateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .FirstOrDefaultAsync(tp => tp.Id == request.TreatmentPlanId, ct);

        if (treatmentPlan == null)
            throw new KeyNotFoundException("Không tìm thấy liệu trình điều trị.");

        treatmentPlan.Update(request.Description, request.EstimatedCost);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(treatmentPlan);
    }

    public async Task<TreatmentPlanDto> DeleteAsync(Guid treatmentPlanId, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct);

        if (treatmentPlan == null)
            throw new KeyNotFoundException("Không tìm thấy liệu trình điều trị.");

        dbContext.TreatmentPlans.Remove(treatmentPlan);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(treatmentPlan);
    }

    private static TreatmentPlanDto ToDto(TreatmentPlan treatmentPlan)
    {
        return new TreatmentPlanDto
        {
            Id = treatmentPlan.Id,
            Description = treatmentPlan.Description,
            Status = treatmentPlan.Status.ToString(),
            EstimatedCost = treatmentPlan.EstimatedCost,
            CreatedAt = treatmentPlan.CreatedAt
        };
    }
}
