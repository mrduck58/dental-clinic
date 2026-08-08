using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

public record ConfirmAppointmentCommand(Guid AppointmentId) : IRequest;

public class ConfirmAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IPatientRepository? patientRepository = null,
    ILogger<ConfirmAppointmentHandler>? logger = null) : IRequestHandler<ConfirmAppointmentCommand>
{
    public async Task Handle(ConfirmAppointmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for Confirm", appointmentId);
            throw new NotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }
        appointment.Confirm();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} confirmed successfully", appointmentId);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Approve,
            module: ActivityModule.Appointment,
            description: $"Xác nhận lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);

        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, AppointmentStatusHelper.VietnamTz);

        // 1. Gửi thông báo cho bác sĩ
        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.Medium,
                Title: "Lịch hẹn đã xác nhận",
                Body: $"Lịch hẹn vào {vnDate:HH:mm dd/MM/yyyy} đã được xác nhận.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }

        // 2. Gửi thông báo cho bệnh nhân (hoặc chủ tài khoản gia đình)
        var patientUserId = await AppointmentStatusHelper.GetPatientUserIdAsync(patientRepository, appointment.PatientId, ct);
        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn đã được xác nhận!",
                Body: $"Lịch hẹn khám nha khoa của bạn vào lúc {vnDate:HH:mm} ngày {vnDate:dd/MM/yyyy} đã được phòng khám xác nhận. Vui lòng đến đúng giờ!",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
    }
}
