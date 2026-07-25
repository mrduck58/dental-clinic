using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreatePrescriptionRequest(
    Guid AppointmentId,
    string? Notes,
    List<PrescriptionItemRequest>? Items);

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

public record UpdatePrescriptionRequest(
    Guid PrescriptionId,
    string? Notes);

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
    DateOnly? StartDate = null);

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
    DateOnly? StartDate = null);

public class PrescriptionHandler(
    AppDbContext dbContext,
    IPatientRepository patientRepository,
    INotificationService notificationService)
{
    public async Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .Include(a => a.Prescriptions)
            .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);

        if (appointment == null)
            throw new KeyNotFoundException("Không tìm thấy lịch hẹn.");

        if (appointment.Status is not (AppointmentStatus.InProgress or AppointmentStatus.PendingPayment or AppointmentStatus.Completed))
            throw new ValidationException("Chỉ có thể tạo đơn thuốc khi buổi hẹn đang khám hoặc đã kết thúc điều trị.");

        // Check if prescription already exists
        var existingPrescription = appointment.Prescriptions.FirstOrDefault();
        if (existingPrescription != null)
            throw new InvalidOperationException("Mỗi cuộc hẹn chỉ có một đơn thuốc. Vui lòng cập nhật đơn thuốc hiện có.");

        var prescription = Prescription.Create(request.AppointmentId, request.Notes);
        dbContext.Prescriptions.Add(prescription);

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
                dbContext.PrescriptionItems.Add(item);
            }
        }

        await dbContext.SaveChangesAsync(ct);

        // Reload with items
        var createdPrescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstAsync(p => p.Id == prescription.Id, ct);

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

        return ToDto(createdPrescription);
    }

    public async Task<PrescriptionDto> UpdateAsync(UpdatePrescriptionRequest request, CancellationToken ct = default)
    {
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, ct);

        if (prescription == null)
            throw new KeyNotFoundException("Không tìm thấy đơn thuốc.");

        prescription.UpdateNotes(request.Notes);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(prescription);
    }

    public async Task<PrescriptionDto> AddItemAsync(AddPrescriptionItemRequest request, CancellationToken ct = default)
    {
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, ct);

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

        dbContext.PrescriptionItems.Add(item);
        await dbContext.SaveChangesAsync(ct);

        // Reload with items
        prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstAsync(p => p.Id == prescription.Id, ct);

        return ToDto(prescription);
    }

    public async Task<PrescriptionDto> UpdateItemAsync(UpdatePrescriptionItemRequest request, CancellationToken ct = default)
    {
        var item = await dbContext.PrescriptionItems.FindAsync(new object[] { request.ItemId }, ct);

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

        await dbContext.SaveChangesAsync(ct);

        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstAsync(p => p.Id == item.PrescriptionId, ct);

        return ToDto(prescription);
    }

    public async Task DeleteItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await dbContext.PrescriptionItems.FindAsync(new object[] { itemId }, ct);

        if (item == null)
            throw new KeyNotFoundException("Không tìm thấy thuốc trong đơn.");

        dbContext.PrescriptionItems.Remove(item);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<PrescriptionDto?> GetByAppointmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var prescription = await dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.AppointmentId == appointmentId, ct);

        return prescription == null ? null : ToDto(prescription);
    }

    private static PrescriptionDto ToDto(Prescription prescription)
    {
        return new PrescriptionDto
        {
            Id = prescription.Id,
            Notes = prescription.Notes,
            CreatedAt = prescription.CreatedAt,
            Items = prescription.Items.Select(i => new PrescriptionItemDto
            {
                Id = i.Id,
                MedicineName = i.MedicineName,
                Dosage = i.Dosage,
                Quantity = i.Quantity,
                Unit = i.Unit,
                Usage = i.Usage,
                Notes = i.Notes,
                TimesPerDay = i.TimesPerDay,
                DurationDays = i.DurationDays,
                StartDate = i.StartDate
            }).ToList()
        };
    }
}
