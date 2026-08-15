using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record UndoNoShowCommand(Guid AppointmentId) : IRequest;

/// <summary>
/// Gỡ một lần ghi nhận vắng mặt bấm nhầm — đối xứng với <see cref="UndoCheckInAppointmentHandler"/>
/// nhưng đơn giản hơn: NoShow chỉ xảy ra từ Confirmed (xem <see cref="Domain.Entities.Appointment.MarkNoShow"/>),
/// nên hoàn tác luôn quay thẳng về đó, không cần phân biệt theo nguồn lịch hẹn.
/// </summary>
public class UndoNoShowHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser) : IRequestHandler<UndoNoShowCommand>
{
    public async Task Handle(UndoNoShowCommand command, CancellationToken ct)
    {
        var appointment = await appointmentRepository.GetByIdAsync(command.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        appointment.UndoNoShow();
        await appointmentRepository.UpdateAsync(appointment, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Hoàn tác ghi nhận vắng mặt lịch hẹn ID: {appointment.Id} — lịch quay về chờ check-in.",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointment.Id.ToString(),
            ct: ct);

        // Bác sĩ vừa nhận thông báo "bệnh nhân vắng mặt" — đính chính để họ không bỏ qua bệnh nhân
        // này khi lễ tân đã sửa lại.
        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, AppointmentStatusHelper.VietnamTz);
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.Medium,
                Title: "Đã hoàn tác ghi nhận vắng mặt",
                Body: $"Lịch hẹn lúc {vnDate:HH:mm dd/MM/yyyy} không còn bị ghi nhận vắng mặt, đang chờ bệnh nhân đến check-in.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }
    }
}
