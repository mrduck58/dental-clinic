using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record EndTreatmentCommand(Guid AppointmentId) : IRequest;

/// <summary>
/// Kết thúc điều trị, chuyển buổi hẹn sang chờ thanh toán — tách ra từ god-handler
/// <c>UpdateAppointmentStatusHandler</c>.
/// <para>
/// LƯU Ý: có đụng nhẹ tới bounded context Reminders — nếu bác sĩ đã hẹn ngày tái khám
/// (<c>FollowUpDate</c>, do <c>SetFollowUpReminderHandler</c> ghi) thì ở đây sinh thông báo
/// nhắc tái khám cho bệnh nhân. Logic giữ nguyên như trước khi tách.
/// </para>
/// </summary>
public class EndTreatmentHandler(
    IAppointmentRepository appointmentRepository,
    INotificationService notificationService,
    IPatientRepository? patientRepository = null,
    ILogger<EndTreatmentHandler>? logger = null) : IRequestHandler<EndTreatmentCommand>
{
    public async Task Handle(EndTreatmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for EndTreatment", appointmentId);
            throw new NotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        if (appointment.Status != AppointmentStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể kết thúc điều trị khi đang trong trạng thái đang khám.");

        appointment.EndTreatment();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} ended treatment, moved to pending payment", appointmentId);

        // Bác sĩ có hẹn tái khám → báo cho bệnh nhân (nếu có tài khoản liên kết).
        if (appointment.FollowUpDate is DateOnly followUpDate && patientRepository != null)
        {
            var patient = await patientRepository.GetByIdAsync(appointment.PatientId, ct);
            if (patient?.UserId is Guid patientUserId)
            {
                var noteSuffix = string.IsNullOrWhiteSpace(appointment.FollowUpNote) ? "" : $" Ghi chú: {appointment.FollowUpNote}";
                await notificationService.CreateAsync(new CreateNotificationRequest(
                    UserId: patientUserId,
                    Type: NotificationType.Reminder,
                    Priority: NotificationPriority.Medium,
                    Title: "Lịch tái khám",
                    Body: $"Bác sĩ hẹn bạn tái khám vào ngày {followUpDate:dd/MM/yyyy}. Vui lòng đặt lịch trước ngày hẹn.{noteSuffix}",
                    RelatedEntityType: "Appointment",
                    RelatedEntityId: appointmentId.ToString()), ct);
            }
        }
    }
}
