using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

public record CheckInAppointmentCommand(Guid AppointmentId) : IRequest;

public class CheckInAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IPatientRepository? patientRepository = null,
    ILogger<CheckInAppointmentHandler>? logger = null) : IRequestHandler<CheckInAppointmentCommand>
{
    public async Task Handle(CheckInAppointmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for CheckIn", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        logger?.LogInformation("CheckIn attempt for {Id}: current status = {Status}", appointmentId, appointment.Status);

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            logger?.LogWarning("CheckIn failed for {Id}: status is {Status}, expected Confirmed", appointmentId, appointment.Status);
            throw new InvalidOperationException($"Chỉ có thể check-in lịch hẹn đã được xác nhận. Trạng thái hiện tại: {appointment.Status}");
        }

        appointment.CheckIn();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} checked in successfully", appointmentId);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Check-in lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);

        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, AppointmentStatusHelper.VietnamTz);

        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Bệnh nhân đã check-in",
                Body: $"Bệnh nhân đã check-in lịch hẹn vào {vnDate:HH:mm dd/MM/yyyy}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }

        var patientUserId = await AppointmentStatusHelper.GetPatientUserIdAsync(patientRepository, appointment.PatientId, ct);
        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.Medium,
                Title: "Check-in thành công",
                Body: $"Bạn đã check-in thành công cho lịch hẹn lúc {vnDate:HH:mm} ngày {vnDate:dd/MM/yyyy}. Vui lòng chờ đến lượt vào khám.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
    }
}
