using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

public record MarkNoShowCommand(Guid AppointmentId) : IRequest;

public class MarkNoShowHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IPatientRepository? patientRepository = null,
    ILogger<MarkNoShowHandler>? logger = null) : IRequestHandler<MarkNoShowCommand>
{
    public async Task Handle(MarkNoShowCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for MarkNoShow", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        // Chỉ ghi nhận vắng với lịch đã xác nhận nhưng chưa check-in — đúng bối cảnh
        // lễ tân dùng ở quầy check-in.
        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            logger?.LogWarning("MarkNoShow failed for {Id}: status is {Status}, expected Confirmed", appointmentId, appointment.Status);
            throw new InvalidOperationException($"Chỉ có thể ghi nhận vắng với lịch hẹn đã xác nhận. Trạng thái hiện tại: {appointment.Status}");
        }

        appointment.MarkNoShow();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} marked as no-show", appointmentId);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Ghi nhận vắng mặt lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);

        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.Medium,
                Title: "Bệnh nhân vắng mặt",
                Body: $"Bệnh nhân không đến khám lịch hẹn vào {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
    }
}
