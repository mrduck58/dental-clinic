using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record CompleteAppointmentCommand(Guid AppointmentId) : IRequest;

/// <summary>Hoàn thành buổi khám — tách ra từ god-handler <c>UpdateAppointmentStatusHandler</c>.</summary>
public class CompleteAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser,
    INotificationService? notificationService = null,
    IPatientRepository? patientRepository = null,
    ILogger<CompleteAppointmentHandler>? logger = null) : IRequestHandler<CompleteAppointmentCommand>
{
    public async Task Handle(CompleteAppointmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for Complete", appointmentId);
            throw new NotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        appointment.Complete();
        await appointmentRepository.UpdateAsync(appointment, ct);

        // Bác sĩ có hẹn tái khám → báo cho bệnh nhân (nếu có tài khoản liên kết).
        if (appointment.FollowUpDate is DateOnly followUpDate && patientRepository != null && notificationService != null)
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

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Hoàn thành lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);
    }
}
