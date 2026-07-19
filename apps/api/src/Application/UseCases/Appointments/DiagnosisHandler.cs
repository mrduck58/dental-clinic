using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateDiagnosisRequest(
    Guid AppointmentId,
    string Description,
    string? GumCondition,
    string? OralMucosaCondition,
    string? GumBleeding,
    string? PainOnChewing,
    string? TeethCount,
    string? DecayedTeeth,
    string? WornOrBrokenTeeth,
    string? LooseTeeth,
    string? Tartar,
    string? Plaque,
    string? BadBreath,
    string? TmjSymptoms,
    string? Occlusion,
    string? OcclusionDeviation,
    string? MedicalHistory,
    string? AllergyHistory,
    string? Conclusion);

public record UpdateDiagnosisRequest(
    Guid DiagnosisId,
    string Description,
    string? GumCondition,
    string? OralMucosaCondition,
    string? GumBleeding,
    string? PainOnChewing,
    string? TeethCount,
    string? DecayedTeeth,
    string? WornOrBrokenTeeth,
    string? LooseTeeth,
    string? Tartar,
    string? Plaque,
    string? BadBreath,
    string? TmjSymptoms,
    string? Occlusion,
    string? OcclusionDeviation,
    string? MedicalHistory,
    string? AllergyHistory,
    string? Conclusion);

public class DiagnosisHandler(AppDbContext dbContext)
{
    public async Task<DiagnosisDto> CreateAsync(CreateDiagnosisRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .Include(a => a.Diagnoses)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment == null)
            throw new KeyNotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (Domain.Enums.AppointmentStatus.InProgress or Domain.Enums.AppointmentStatus.PendingPayment or Domain.Enums.AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể thêm chuẩn đoán khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var diagnosis = Diagnosis.Create(request.AppointmentId, request.Description, ToDetails(request));

        dbContext.Diagnoses.Add(diagnosis);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(diagnosis);
    }

    public async Task<DiagnosisDto> UpdateAsync(UpdateDiagnosisRequest request, CancellationToken ct = default)
    {
        var diagnosis = await dbContext.Diagnoses.FindAsync(new object[] { request.DiagnosisId }, ct);

        if (diagnosis == null)
            throw new KeyNotFoundException("Không tìm thấy chuẩn đoán.");

        diagnosis.Update(request.Description, ToDetails(request));
        await dbContext.SaveChangesAsync(ct);

        return ToDto(diagnosis);
    }

    public async Task DeleteAsync(Guid diagnosisId, CancellationToken ct = default)
    {
        var diagnosis = await dbContext.Diagnoses.FindAsync(new object[] { diagnosisId }, ct);

        if (diagnosis == null)
            throw new KeyNotFoundException("Không tìm thấy chuẩn đoán.");

        dbContext.Diagnoses.Remove(diagnosis);
        await dbContext.SaveChangesAsync(ct);
    }

    private static DiagnosisDetails ToDetails(CreateDiagnosisRequest r) => new(
        Norm(r.GumCondition), Norm(r.OralMucosaCondition), Norm(r.GumBleeding), Norm(r.PainOnChewing),
        Norm(r.TeethCount), Norm(r.DecayedTeeth), Norm(r.WornOrBrokenTeeth), Norm(r.LooseTeeth),
        Norm(r.Tartar), Norm(r.Plaque), Norm(r.BadBreath),
        Norm(r.TmjSymptoms), Norm(r.Occlusion), Norm(r.OcclusionDeviation),
        Norm(r.MedicalHistory), Norm(r.AllergyHistory), Norm(r.Conclusion));

    private static DiagnosisDetails ToDetails(UpdateDiagnosisRequest r) => new(
        Norm(r.GumCondition), Norm(r.OralMucosaCondition), Norm(r.GumBleeding), Norm(r.PainOnChewing),
        Norm(r.TeethCount), Norm(r.DecayedTeeth), Norm(r.WornOrBrokenTeeth), Norm(r.LooseTeeth),
        Norm(r.Tartar), Norm(r.Plaque), Norm(r.BadBreath),
        Norm(r.TmjSymptoms), Norm(r.Occlusion), Norm(r.OcclusionDeviation),
        Norm(r.MedicalHistory), Norm(r.AllergyHistory), Norm(r.Conclusion));

    private static string? Norm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static DiagnosisDto ToDto(Diagnosis d)
    {
        return new DiagnosisDto
        {
            Id = d.Id,
            Description = d.Description,
            GumCondition = d.GumCondition,
            OralMucosaCondition = d.OralMucosaCondition,
            GumBleeding = d.GumBleeding,
            PainOnChewing = d.PainOnChewing,
            TeethCount = d.TeethCount,
            DecayedTeeth = d.DecayedTeeth,
            WornOrBrokenTeeth = d.WornOrBrokenTeeth,
            LooseTeeth = d.LooseTeeth,
            Tartar = d.Tartar,
            Plaque = d.Plaque,
            BadBreath = d.BadBreath,
            TmjSymptoms = d.TmjSymptoms,
            Occlusion = d.Occlusion,
            OcclusionDeviation = d.OcclusionDeviation,
            MedicalHistory = d.MedicalHistory,
            AllergyHistory = d.AllergyHistory,
            Conclusion = d.Conclusion,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };
    }
}
