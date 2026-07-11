using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateDiagnosisRequest(
    Guid AppointmentId,
    string DiagnosisCode,
    string Description,
    string? Notes,
    decimal? HeartRate,
    decimal? Temperature,
    decimal? BloodPressureSystolic,
    decimal? BloodPressureDiastolic,
    string? MedicalHistory,
    string? AllergyHistory,
    string? DentalCondition,
    string? Conclusion);

public record UpdateDiagnosisRequest(
    Guid DiagnosisId,
    string DiagnosisCode,
    string Description,
    string? Notes,
    decimal? HeartRate,
    decimal? Temperature,
    decimal? BloodPressureSystolic,
    decimal? BloodPressureDiastolic,
    string? MedicalHistory,
    string? AllergyHistory,
    string? DentalCondition,
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

        if (appointment.Status != Domain.Enums.AppointmentStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể thêm chuẩn đoán khi cuộc hẹn đang trong trạng thái đang khám.");

        var diagnosis = Diagnosis.Create(
            request.AppointmentId,
            request.DiagnosisCode,
            request.Description,
            request.Notes,
            request.HeartRate,
            request.Temperature,
            request.BloodPressureSystolic,
            request.BloodPressureDiastolic,
            request.MedicalHistory,
            request.AllergyHistory,
            request.DentalCondition,
            request.Conclusion);

        dbContext.Diagnoses.Add(diagnosis);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(diagnosis);
    }

    public async Task<DiagnosisDto> UpdateAsync(UpdateDiagnosisRequest request, CancellationToken ct = default)
    {
        var diagnosis = await dbContext.Diagnoses.FindAsync(new object[] { request.DiagnosisId }, ct);

        if (diagnosis == null)
            throw new KeyNotFoundException("Không tìm thấy chuẩn đoán.");

        diagnosis.Update(
            request.DiagnosisCode,
            request.Description,
            request.Notes,
            request.HeartRate,
            request.Temperature,
            request.BloodPressureSystolic,
            request.BloodPressureDiastolic,
            request.MedicalHistory,
            request.AllergyHistory,
            request.DentalCondition,
            request.Conclusion);
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

    public static DiagnosisDto ToDto(Diagnosis diagnosis)
    {
        return new DiagnosisDto
        {
            Id = diagnosis.Id,
            DiagnosisCode = diagnosis.DiagnosisCode,
            Description = diagnosis.Description,
            Notes = diagnosis.Notes,
            HeartRate = diagnosis.HeartRate,
            Temperature = diagnosis.Temperature,
            BloodPressureSystolic = diagnosis.BloodPressureSystolic,
            BloodPressureDiastolic = diagnosis.BloodPressureDiastolic,
            MedicalHistory = diagnosis.MedicalHistory,
            AllergyHistory = diagnosis.AllergyHistory,
            DentalCondition = diagnosis.DentalCondition,
            Conclusion = diagnosis.Conclusion,
            CreatedAt = diagnosis.CreatedAt,
            UpdatedAt = diagnosis.UpdatedAt
        };
    }
}
