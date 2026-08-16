using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record UndoCheckInCommand(Guid AppointmentId) : IRequest<UndoCheckInResult>;

/// <param name="Origin">Nguồn lịch hẹn — client dùng để hiện đúng thông báo đã xảy ra chuyện gì.</param>
/// <param name="Status">Trạng thái sau khi hoàn tác: "Confirmed" (quay về chờ check-in).</param>
public record UndoCheckInResult(Guid AppointmentId, string Origin, string Status);

/// <summary>
/// Gỡ một lần check-in bấm nhầm. Nút check-in nằm ngay trên thẻ bệnh nhân trong danh sách chờ nên
/// bấm nhầm người bên cạnh là chuyện xảy ra thật; sau khi hoàn tác, lịch hẹn quay về trạng thái
/// Confirmed (danh sách Đang chờ check-in) để lễ tân có thể thực hiện lại khi bệnh nhân đến.
///
/// Handler này lo phần ghi nhật ký và thông báo cho bác sĩ và bệnh nhân.
/// </summary>
public class UndoCheckInAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IRequestHandler<UndoCheckInCommand, UndoCheckInResult>
{
    public async Task<UndoCheckInResult> Handle(UndoCheckInCommand command, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdAsync(command.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var origin = appointment.Origin;

        appointment.UndoCheckIn(currentUser.UserId, DateTimeOffset.UtcNow);
        await appointmentRepository.UpdateAsync(appointment, ct);

        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, AppointmentStatusHelper.VietnamTz);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Hoàn tác check-in lịch hẹn ID: {appointment.Id} — lịch quay về danh sách chờ check-in.",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointment.Id.ToString(),
            ct: ct);

        await NotifyAsync(appointment, origin, vnDate, ct);

        return new UndoCheckInResult(
            appointment.Id,
            origin.ToString(),
            appointment.Status.ToString());
    }

    private async Task NotifyAsync(
        Domain.Entities.Appointment appointment,
        AppointmentOrigin origin,
        DateTimeOffset vnDate,
        CancellationToken ct)
    {
        // Bác sĩ vừa nhận thông báo "bệnh nhân đã check-in" và có thể đang chờ người này —
        // thông báo đính chính đưa bệnh nhân khỏi hàng đợi.
        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Check-in đã được hoàn tác",
                Body: $"Bệnh nhân của lịch hẹn lúc {vnDate:HH:mm dd/MM/yyyy} đã được đưa khỏi hàng đợi do thao tác hoàn tác check-in.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }

        // Bệnh nhân đã nhận thông báo "check-in thành công" ở bước trước, kể cả khi lễ tân bấm nhầm
        // người — thông báo hoàn tác để họ biết lịch hẹn đang ở trạng thái chờ check-in lại.
        var patientUserId = await AppointmentStatusHelper.GetPatientUserIdAsync(
            patientRepository, appointment.PatientId, ct);

        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty && patientUserId.Value != currentUser.UserId)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Check-in đã được hoàn tác",
                Body: $"Lượt check-in cho lịch hẹn lúc {vnDate:HH:mm dd/MM/yyyy} đã được hoàn tác do thao tác nhầm tại quầy. Lịch hẹn quay về trạng thái chờ check-in tại phòng khám.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }
    }
}
