using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

// ── Request/Command ──────────────────────────────────────────────────────────
// Hình dạng JSON body giữ NGUYÊN như trước khi tách god-handler PrescriptionHandler.

public record PrescriptionItemRequest(
    string MedicineName,
    string Dosage,
    int Quantity,
    string Unit,
    string Usage,
    string? Notes,
    int? TimesPerDay = null,
    int? DurationDays = null,
    DateOnly? StartDate = null);

public record CreatePrescriptionRequest(
    Guid AppointmentId,
    string? Notes,
    List<PrescriptionItemRequest>? Items) : IRequest<PrescriptionDto>;

public record UpdatePrescriptionRequest(
    Guid PrescriptionId,
    string? Notes) : IRequest<PrescriptionDto>;

public record AddPrescriptionItemRequest(
    Guid PrescriptionId,
    string MedicineName,
    string Dosage,
    int Quantity,
    string Unit,
    string Usage,
    string? Notes,
    int? TimesPerDay = null,
    int? DurationDays = null,
    DateOnly? StartDate = null) : IRequest<PrescriptionDto>;

public record UpdatePrescriptionItemRequest(
    Guid ItemId,
    string MedicineName,
    string Dosage,
    int Quantity,
    string Unit,
    string Usage,
    string? Notes,
    int? TimesPerDay = null,
    int? DurationDays = null,
    DateOnly? StartDate = null) : IRequest<PrescriptionDto>;

public record DeletePrescriptionItemCommand(Guid ItemId) : IRequest;

public record GetPrescriptionByAppointmentQuery(Guid AppointmentId) : IRequest<PrescriptionDto?>;

// ── Handlers ─────────────────────────────────────────────────────────────────

public class CreatePrescriptionHandler(
    IAppointmentRepository appointmentRepository,
    IPrescriptionRepository prescriptionRepository,
    IPrescriptionItemRepository prescriptionItemRepository,
    IPatientRepository patientRepository,
    INotificationService notificationService) : IRequestHandler<CreatePrescriptionRequest, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(CreatePrescriptionRequest request, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, ct);

        if (appointment == null)
            throw new KeyNotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể tạo đơn thuốc khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        // Check if prescription already exists
        var existingPrescription = await prescriptionRepository.GetByAppointmentIdWithItemsAsync(request.AppointmentId, ct);
        if (existingPrescription != null)
            throw new InvalidOperationException("Mỗi cuộc hẹn chỉ có một đơn thuốc. Vui lòng cập nhật đơn thuốc hiện có.");

        var prescription = Prescription.Create(request.AppointmentId, request.Notes);
        await prescriptionRepository.AddAsync(prescription, ct);

        if (request.Items != null)
        {
            foreach (var itemRequest in request.Items)
            {
                var item = PrescriptionItem.Create(
                    prescription.Id,
                    itemRequest.MedicineName,
                    itemRequest.Dosage,
                    itemRequest.Quantity,
                    itemRequest.Unit,
                    itemRequest.Usage,
                    itemRequest.Notes,
                    itemRequest.TimesPerDay,
                    itemRequest.DurationDays,
                    itemRequest.StartDate);
                await prescriptionItemRepository.AddAsync(item, ct);
            }
        }

        // Reload with items
        var createdPrescription = await prescriptionRepository.GetByIdWithItemsAsync(prescription.Id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn thuốc.");

        // Báo cho bệnh nhân có đơn thuốc mới (nếu tài khoản có liên kết User) — trước đây bệnh nhân
        // chỉ biết đơn thuốc khi tự vào xem hồ sơ, không được chủ động báo.
        var patient = await patientRepository.GetByIdAsync(appointment.PatientId, ct);
        if (patient != null && patient.UserId != Guid.Empty)
        {
            var patientUserId = patient.UserId;
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId,
                Type: NotificationType.Service,
                Priority: NotificationPriority.Medium,
                Title: "Đơn thuốc mới",
                Body: "Bác sĩ đã kê đơn thuốc mới cho bạn. Xem chi tiết trong hồ sơ khám bệnh.",
                RelatedEntityType: "Prescription",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }

        return ClinicalRecordMappers.ToDto(createdPrescription);
    }
}

public class UpdatePrescriptionHandler(IPrescriptionRepository prescriptionRepository)
    : IRequestHandler<UpdatePrescriptionRequest, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(UpdatePrescriptionRequest request, CancellationToken ct)
    {
        var prescription = await prescriptionRepository.GetByIdWithItemsAsync(request.PrescriptionId, ct);

        if (prescription == null)
            throw new KeyNotFoundException("Không tìm thấy đơn thuốc.");

        prescription.UpdateNotes(request.Notes);
        await prescriptionRepository.UpdateAsync(prescription, ct);

        return ClinicalRecordMappers.ToDto(prescription);
    }
}

public class AddPrescriptionItemHandler(
    IPrescriptionRepository prescriptionRepository,
    IPrescriptionItemRepository prescriptionItemRepository) : IRequestHandler<AddPrescriptionItemRequest, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(AddPrescriptionItemRequest request, CancellationToken ct)
    {
        var prescription = await prescriptionRepository.GetByIdWithItemsAsync(request.PrescriptionId, ct);

        if (prescription == null)
            throw new KeyNotFoundException("Không tìm thấy đơn thuốc.");

        var item = PrescriptionItem.Create(
            request.PrescriptionId,
            request.MedicineName,
            request.Dosage,
            request.Quantity,
            request.Unit,
            request.Usage,
            request.Notes,
            request.TimesPerDay,
            request.DurationDays,
            request.StartDate);

        await prescriptionItemRepository.AddAsync(item, ct);

        // Reload with items
        prescription = await prescriptionRepository.GetByIdWithItemsAsync(prescription.Id, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn thuốc.");

        return ClinicalRecordMappers.ToDto(prescription);
    }
}

public class UpdatePrescriptionItemHandler(
    IPrescriptionItemRepository prescriptionItemRepository,
    IPrescriptionRepository prescriptionRepository) : IRequestHandler<UpdatePrescriptionItemRequest, PrescriptionDto>
{
    public async Task<PrescriptionDto> Handle(UpdatePrescriptionItemRequest request, CancellationToken ct)
    {
        var item = await prescriptionItemRepository.GetByIdAsync(request.ItemId, ct);

        if (item == null)
            throw new KeyNotFoundException("Không tìm thấy thuốc trong đơn.");

        item.Update(
            request.MedicineName,
            request.Dosage,
            request.Quantity,
            request.Unit,
            request.Usage,
            request.Notes,
            request.TimesPerDay,
            request.DurationDays,
            request.StartDate);

        await prescriptionItemRepository.UpdateAsync(item, ct);

        var prescription = await prescriptionRepository.GetByIdWithItemsAsync(item.PrescriptionId, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn thuốc.");

        return ClinicalRecordMappers.ToDto(prescription);
    }
}

public class DeletePrescriptionItemHandler(IPrescriptionItemRepository prescriptionItemRepository)
    : IRequestHandler<DeletePrescriptionItemCommand>
{
    public async Task Handle(DeletePrescriptionItemCommand command, CancellationToken ct)
    {
        var item = await prescriptionItemRepository.GetByIdAsync(command.ItemId, ct);

        if (item == null)
            throw new KeyNotFoundException("Không tìm thấy thuốc trong đơn.");

        await prescriptionItemRepository.DeleteAsync(item, ct);
    }
}

public class GetPrescriptionByAppointmentHandler(IPrescriptionRepository prescriptionRepository)
    : IRequestHandler<GetPrescriptionByAppointmentQuery, PrescriptionDto?>
{
    public async Task<PrescriptionDto?> Handle(GetPrescriptionByAppointmentQuery request, CancellationToken ct)
    {
        var prescription = await prescriptionRepository.GetByAppointmentIdWithItemsAsync(request.AppointmentId, ct);

        return prescription == null ? null : ClinicalRecordMappers.ToDto(prescription);
    }
}
