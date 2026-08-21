using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

/// <param name="Reason">Nhóm lý do, để thống kê được vì sao bệnh nhân bỏ lịch.</param>
/// <param name="Note">Ghi chú tự do; bắt buộc khi Reason = Other.</param>
public record CancelAppointmentCommand(
    Guid AppointmentId,
    CancellationReason Reason,
    string? Note) : IRequest;

public class CancelAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IPatientRepository patientRepository,
    AppointmentChangeGuard changeGuard,
    ILogger<CancelAppointmentHandler>? logger = null) : IRequestHandler<CancelAppointmentCommand>
{
    public async Task Handle(CancelAppointmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;
        var now = DateTimeOffset.UtcNow;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        await changeGuard.AuthorizeAsync(appointment, now, ct, isCancelOperation: true);

        // Entity tự chặn các trạng thái không hủy được (đang khám, đã hoàn thành...).
        appointment.Cancel(command.Reason, command.Note, currentUser.UserId, now);
        await appointmentRepository.UpdateAsync(appointment, ct);

        var reasonText = DescribeReason(command.Reason, command.Note);
        logger?.LogInformation("Appointment {Id} cancelled: {Reason}", appointmentId, reasonText);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Cancel,
            module: ActivityModule.Appointment,
            description: $"Hủy lịch hẹn ID: {appointmentId}. Lý do: {reasonText}",
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
                Body: $"Lịch hẹn vào {vnDate:HH:mm dd/MM/yyyy} đã bị hủy. Lý do: {reasonText}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }

        // Không tự gửi thông báo về cho chính người vừa bấm hủy — họ vừa thấy màn hình xác nhận rồi.
        var patientUserId = await AppointmentStatusHelper.GetPatientUserIdAsync(patientRepository, appointment.PatientId, ct);
        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty && patientUserId.Value != currentUser.UserId)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn đã bị hủy",
                Body: $"Lịch hẹn khám vào lúc {vnDate:HH:mm} ngày {vnDate:dd/MM/yyyy} đã bị hủy. Lý do: {reasonText}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
    }

    /// <summary>Nhãn tiếng Việt của nhóm lý do, kèm ghi chú tự do nếu có — dùng cho nhật ký và thông báo.</summary>
    private static string DescribeReason(CancellationReason reason, string? note)
    {
        var label = CancellationReasonCatalog.LabelOf(reason);
        return string.IsNullOrWhiteSpace(note) ? label : $"{label} — {note.Trim()}";
    }
}
