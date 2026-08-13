using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

// Hình dạng JSON của body giữ NGUYÊN như trước khi tách (frontend đang hardcode) — các record
// request cũ nay chính là Command/Query của MediatR.

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
    string? Conclusion) : IRequest<DiagnosisDto>;

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
    string? Conclusion) : IRequest<DiagnosisDto>;

public record DeleteDiagnosisCommand(Guid DiagnosisId) : IRequest;

/// <summary>Chuẩn hóa các trường chi tiết của phiếu khám — dùng chung cho create/update.</summary>
internal static class DiagnosisDetailsMapper
{
    private static string? Norm(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static DiagnosisDetails ToDetails(CreateDiagnosisRequest r) => new(
        Norm(r.GumCondition), Norm(r.OralMucosaCondition), Norm(r.GumBleeding), Norm(r.PainOnChewing),
        Norm(r.TeethCount), Norm(r.DecayedTeeth), Norm(r.WornOrBrokenTeeth), Norm(r.LooseTeeth),
        Norm(r.Tartar), Norm(r.Plaque), Norm(r.BadBreath),
        Norm(r.TmjSymptoms), Norm(r.Occlusion), Norm(r.OcclusionDeviation),
        Norm(r.MedicalHistory), Norm(r.AllergyHistory), Norm(r.Conclusion));

    public static DiagnosisDetails ToDetails(UpdateDiagnosisRequest r) => new(
        Norm(r.GumCondition), Norm(r.OralMucosaCondition), Norm(r.GumBleeding), Norm(r.PainOnChewing),
        Norm(r.TeethCount), Norm(r.DecayedTeeth), Norm(r.WornOrBrokenTeeth), Norm(r.LooseTeeth),
        Norm(r.Tartar), Norm(r.Plaque), Norm(r.BadBreath),
        Norm(r.TmjSymptoms), Norm(r.Occlusion), Norm(r.OcclusionDeviation),
        Norm(r.MedicalHistory), Norm(r.AllergyHistory), Norm(r.Conclusion));
}

public class CreateDiagnosisHandler(
    IAppointmentRepository appointmentRepository,
    IDiagnosisRepository diagnosisRepository) : IRequestHandler<CreateDiagnosisRequest, DiagnosisDto>
{
    public async Task<DiagnosisDto> Handle(CreateDiagnosisRequest request, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, ct);

        if (appointment == null)
            throw new NotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể thêm chuẩn đoán khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        var diagnosis = Diagnosis.Create(request.AppointmentId, request.Description, DiagnosisDetailsMapper.ToDetails(request));

        await diagnosisRepository.AddAsync(diagnosis, ct);

        return ClinicalRecordMappers.ToDto(diagnosis);
    }
}

public class UpdateDiagnosisHandler(IDiagnosisRepository diagnosisRepository) : IRequestHandler<UpdateDiagnosisRequest, DiagnosisDto>
{
    public async Task<DiagnosisDto> Handle(UpdateDiagnosisRequest request, CancellationToken ct)
    {
        var diagnosis = await diagnosisRepository.GetByIdAsync(request.DiagnosisId, ct);

        if (diagnosis == null)
            throw new NotFoundException("Không tìm thấy chuẩn đoán.");

        diagnosis.Update(request.Description, DiagnosisDetailsMapper.ToDetails(request));
        await diagnosisRepository.UpdateAsync(diagnosis, ct);

        return ClinicalRecordMappers.ToDto(diagnosis);
    }
}

public class DeleteDiagnosisHandler(IDiagnosisRepository diagnosisRepository) : IRequestHandler<DeleteDiagnosisCommand>
{
    public async Task Handle(DeleteDiagnosisCommand command, CancellationToken ct)
    {
        var diagnosis = await diagnosisRepository.GetByIdAsync(command.DiagnosisId, ct);

        if (diagnosis == null)
            throw new NotFoundException("Không tìm thấy chuẩn đoán.");

        await diagnosisRepository.DeleteAsync(diagnosis, ct);
    }
}
