using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateTreatmentPlanRequest(
    Guid AppointmentId,
    string Description,
    decimal? EstimatedCost,
    List<TreatmentPlanStepRequest>? Steps);

public record TreatmentPlanStepRequest(
    int StepNumber,
    string Description,
    string? Notes);

public record UpdateTreatmentPlanRequest(
    Guid TreatmentPlanId,
    string Description,
    decimal? EstimatedCost);

public record AddTreatmentStepRequest(
    Guid TreatmentPlanId,
    int StepNumber,
    string Description,
    string? Notes);

public record UpdateTreatmentStepRequest(
    Guid StepId,
    string Description,
    string? Notes);

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

        if (request.Steps != null)
        {
            foreach (var stepRequest in request.Steps)
            {
                var step = TreatmentPlanStep.Create(
                    treatmentPlan.Id,
                    stepRequest.StepNumber,
                    stepRequest.Description,
                    stepRequest.Notes);
                dbContext.TreatmentPlanSteps.Add(step);
            }
        }

        await dbContext.SaveChangesAsync(ct);

        // Reload with steps
        var createdPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Steps)
            .FirstAsync(tp => tp.Id == treatmentPlan.Id, ct);

        return ToDto(createdPlan);
    }

    public async Task<TreatmentPlanDto> UpdateAsync(UpdateTreatmentPlanRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Steps)
            .FirstOrDefaultAsync(tp => tp.Id == request.TreatmentPlanId, ct);

        if (treatmentPlan == null)
            throw new KeyNotFoundException("Không tìm thấy liệu trình điều trị.");

        treatmentPlan.Update(request.Description, request.EstimatedCost);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(treatmentPlan);
    }

    public async Task<TreatmentPlanDto> AddStepAsync(AddTreatmentStepRequest request, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Steps)
            .FirstOrDefaultAsync(tp => tp.Id == request.TreatmentPlanId, ct);

        if (treatmentPlan == null)
            throw new KeyNotFoundException("Không tìm thấy liệu trình điều trị.");

        var step = TreatmentPlanStep.Create(
            request.TreatmentPlanId,
            request.StepNumber,
            request.Description,
            request.Notes);

        dbContext.TreatmentPlanSteps.Add(step);
        await dbContext.SaveChangesAsync(ct);

        // Reload with steps
        treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Steps)
            .FirstAsync(tp => tp.Id == treatmentPlan.Id, ct);

        return ToDto(treatmentPlan);
    }

    public async Task<TreatmentPlanDto> UpdateStepAsync(UpdateTreatmentStepRequest request, CancellationToken ct = default)
    {
        var step = await dbContext.TreatmentPlanSteps.FindAsync(new object[] { request.StepId }, ct);

        if (step == null)
            throw new KeyNotFoundException("Không tìm thấy bước điều trị.");

        step.Update(request.Description, request.Notes);
        await dbContext.SaveChangesAsync(ct);

        var treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Steps)
            .FirstAsync(tp => tp.Id == step.TreatmentPlanId, ct);

        return ToDto(treatmentPlan);
    }

    public async Task<TreatmentPlanDto> CompleteStepAsync(Guid stepId, CancellationToken ct = default)
    {
        var step = await dbContext.TreatmentPlanSteps.FindAsync(new object[] { stepId }, ct);

        if (step == null)
            throw new KeyNotFoundException("Không tìm thấy bước điều trị.");

        step.Complete();
        await dbContext.SaveChangesAsync(ct);

        var treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Steps)
            .FirstAsync(tp => tp.Id == step.TreatmentPlanId, ct);

        return ToDto(treatmentPlan);
    }

    public async Task<TreatmentPlanDto> DeleteAsync(Guid treatmentPlanId, CancellationToken ct = default)
    {
        var treatmentPlan = await dbContext.TreatmentPlans
            .Include(tp => tp.Steps)
            .FirstOrDefaultAsync(tp => tp.Id == treatmentPlanId, ct);

        if (treatmentPlan == null)
            throw new KeyNotFoundException("Không tìm thấy liệu trình điều trị.");

        dbContext.TreatmentPlans.Remove(treatmentPlan);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(treatmentPlan);
    }

    public async Task DeleteStepAsync(Guid stepId, CancellationToken ct = default)
    {
        var step = await dbContext.TreatmentPlanSteps.FindAsync(new object[] { stepId }, ct);

        if (step == null)
            throw new KeyNotFoundException("Không tìm thấy bước điều trị.");

        dbContext.TreatmentPlanSteps.Remove(step);
        await dbContext.SaveChangesAsync(ct);
    }

    private static TreatmentPlanDto ToDto(TreatmentPlan treatmentPlan)
    {
        return new TreatmentPlanDto
        {
            Id = treatmentPlan.Id,
            Description = treatmentPlan.Description,
            Status = treatmentPlan.Status.ToString(),
            EstimatedCost = treatmentPlan.EstimatedCost,
            CreatedAt = treatmentPlan.CreatedAt,
            Steps = treatmentPlan.Steps.OrderBy(s => s.StepNumber).Select(s => new TreatmentPlanStepDto
            {
                Id = s.Id,
                StepNumber = s.StepNumber,
                Description = s.Description,
                Status = s.Status.ToString(),
                Notes = s.Notes
            }).ToList()
        };
    }
}
