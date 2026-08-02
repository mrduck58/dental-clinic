using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

public record CancelAppointmentCommand(Guid AppointmentId, string? Reason = null) : IRequest;

public class CancelAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IPatientRepository? patientRepository = null,
    ILogger<CancelAppointmentHandler>? logger = null) : IRequestHandler<CancelAppointmentCommand>
{
    public async Task Handle(CancelAppointmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;
        var reason = command.Reason;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for Cancel", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        // Validate permissions for Patient role
        if (currentUser.IsAuthenticated && currentUser.UserRole == "Patient")
        {
            if (patientRepository == null)
            {
                throw new InvalidOperationException("Chưa cấu hình repository của bệnh nhân.");
            }

            if (currentUser.UserId == null)
            {
                throw new UnauthorizedAccessException("Không xác định được ID người dùng.");
            }

            var patient = await patientRepository.GetByUserIdAsync(currentUser.UserId.Value, ct);
            if (patient == null)
            {
                throw new UnauthorizedAccessException("Không tìm thấy hồ sơ bệnh nhân tương ứng với tài khoản.");
            }

            if (appointment.PatientId != patient.Id)
            {
                // Check if this appointment belongs to a family member
                var familyMembers = await patientRepository.GetFamilyMembersAsync(patient.Id, ct);
                if (!familyMembers.Any(f => f.Id == appointment.PatientId))
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền hủy lịch hẹn này.");
                }
            }
        }

        appointment.Cancel(reason);
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} cancelled with reason: {Reason}", appointmentId, reason ?? "N/A");

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Cancel,
            module: ActivityModule.Appointment,
            description: $"Hủy lịch hẹn ID: {appointmentId}. Lý do: {reason ?? "Không có"}",
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
                Title: "Lịch hẹn bị hủy",
                Body: $"Lịch hẹn vào {vnDate:HH:mm dd/MM/yyyy} đã bị hủy. Lý do: {reason ?? "Không có"}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }

        var patientUserId = await AppointmentStatusHelper.GetPatientUserIdAsync(patientRepository, appointment.PatientId, ct);
        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty && patientUserId.Value != currentUser.UserId)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn đã bị hủy",
                Body: $"Lịch hẹn khám vào lúc {vnDate:HH:mm} ngày {vnDate:dd/MM/yyyy} đã bị hủy. Lý do: {reason ?? "Theo yêu cầu"}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
    }
}
