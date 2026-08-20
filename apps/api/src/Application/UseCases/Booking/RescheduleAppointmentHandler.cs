using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

/// <param name="DentistId">Bỏ trống để giữ nguyên bác sĩ hiện tại.</param>
/// <param name="ServiceId">Bỏ trống để giữ nguyên dịch vụ hiện tại.</param>
public record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTimeOffset AppointmentDate,
    Guid? DentistId,
    Guid? ServiceId,
    string? Reason) : IRequest<RescheduleAppointmentResult>;

public record RescheduleAppointmentResult(
    Guid AppointmentId,
    DateTimeOffset AppointmentDate,
    Guid DentistId,
    string Status,
    int RescheduledCount);

/// <summary>
/// Dời lịch bằng cách SỬA TẠI CHỖ bản ghi hiện có, không phải hủy-rồi-tạo-mới. Giữ nguyên
/// AppointmentId nên hóa đơn, bệnh án, thông báo và hàng đợi đang trỏ vào nó không bị đứt; đồng thời
/// mỗi lần dời không để lại một bản ghi Cancelled làm tỉ lệ hủy trong báo cáo phồng lên giả tạo.
/// Lịch sử dời vẫn còn qua RescheduledCount / LastRescheduledAt.
/// </summary>
public class RescheduleAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    AppointmentChangeGuard changeGuard,
    AppointmentSlotGuard slotGuard,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    ISlotHoldRepository? slotHoldRepository = null) : IRequestHandler<RescheduleAppointmentCommand, RescheduleAppointmentResult>
{
    public async Task<RescheduleAppointmentResult> Handle(RescheduleAppointmentCommand command, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var appointment = await appointmentRepository.GetByIdAsync(command.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var context = await changeGuard.AuthorizeAsync(appointment, now, ct);

        if (context.IsPatientCaller &&
            appointment.RescheduledCount >= AppointmentChangeGuard.MaxPatientReschedules)
        {
            throw new ConflictException(
                $"Bạn chỉ được dời lịch tối đa {AppointmentChangeGuard.MaxPatientReschedules} lần. " +
                "Vui lòng liên hệ phòng khám để được hỗ trợ.");
        }

        if (command.AppointmentDate.AddMinutes(SlotCalculator.WalkInGraceMinutes) <= now)
            throw new ValidationException("Không thể dời lịch về thời điểm trong quá khứ.");

        var previousDentistId = appointment.DentistId;
        var previousDate = appointment.AppointmentDate;

        var newDentistId = command.DentistId ?? appointment.DentistId;
        var newServiceId = command.ServiceId ?? appointment.ServiceId;

        var localNewDate = DateOnly.FromDateTime(command.AppointmentDate.UtcDateTime.AddHours(7));
        var hasActiveAppointment = await appointmentRepository.HasActiveAppointmentOnDateAsync(
            appointment.PatientId, localNewDate, excludeAppointmentId: appointment.Id, ct);
        if (hasActiveAppointment)
        {
            throw new ConflictException("Bệnh nhân này đã có một lịch hẹn khác trong ngày này. Mỗi bệnh nhân chỉ được đặt tối đa 1 lịch hẹn mỗi ngày (nếu muốn đổi giờ, vui lòng dời hoặc hủy lịch cũ).");
        }

        await slotGuard.EnsureSlotAvailableAsync(
            newDentistId, command.AppointmentDate, newServiceId,
            excludeAppointmentId: appointment.Id, ct);

        // Bệnh nhân tự dời thì lịch quay về Pending để phòng khám sắp xếp lại; nhân viên dời thì
        // giữ nguyên trạng thái vì chính họ đang là người sắp xếp.
        appointment.Reschedule(
            command.AppointmentDate, newDentistId, newServiceId,
            requiresReconfirmation: context.IsPatientCaller, now);

        await appointmentRepository.UpdateAsync(appointment, ct);

        // Xác nhận và tiêu thụ lượt giữ chỗ sau khi dời lịch thành công
        if (slotHoldRepository != null)
        {
            var hold = await slotHoldRepository.GetActiveHoldForSlotAsync(newDentistId, command.AppointmentDate, now, ct);
            if (hold != null && (hold.PatientId == appointment.PatientId || hold.UserId == currentUser.UserId))
            {
                hold.Confirm();
                await slotHoldRepository.UpdateAsync(hold, ct);
            }
        }

        await NotifyAsync(appointment, previousDentistId, previousDate, command.Reason, ct);

        var vnNew = TimeZoneInfo.ConvertTime(command.AppointmentDate, AppointmentStatusHelper.VietnamTz);
        var vnOld = TimeZoneInfo.ConvertTime(previousDate, AppointmentStatusHelper.VietnamTz);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Dời lịch hẹn ID: {appointment.Id} từ {vnOld:HH:mm dd/MM/yyyy} sang {vnNew:HH:mm dd/MM/yyyy}." +
                         (string.IsNullOrWhiteSpace(command.Reason) ? "" : $" Lý do: {command.Reason.Trim()}"),
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointment.Id.ToString(),
            ct: ct);

        return new RescheduleAppointmentResult(
            appointment.Id,
            appointment.AppointmentDate,
            appointment.DentistId,
            appointment.Status.ToString(),
            appointment.RescheduledCount);
    }

    private async Task NotifyAsync(
        Domain.Entities.Appointment appointment,
        Guid previousDentistId,
        DateTimeOffset previousDate,
        string? reason,
        CancellationToken ct)
    {
        var vnNew = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, AppointmentStatusHelper.VietnamTz);
        var vnOld = TimeZoneInfo.ConvertTime(previousDate, AppointmentStatusHelper.VietnamTz);
        var reasonSuffix = string.IsNullOrWhiteSpace(reason) ? "" : $" Lý do: {reason.Trim()}.";

        // Bác sĩ mới cần biết mình có thêm ca.
        var newDentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (newDentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: newDentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn được dời",
                Body: $"Một lịch hẹn đã được dời sang {vnNew:HH:mm dd/MM/yyyy}.{reasonSuffix}",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }

        // Bác sĩ cũ cần biết ca đã rời khỏi lịch của mình — nếu không, khung giờ đó vẫn nằm trong đầu họ.
        if (previousDentistId != appointment.DentistId)
        {
            var oldDentistUserId = await appointmentRepository.GetDentistUserIdAsync(previousDentistId, ct);
            if (oldDentistUserId.HasValue)
            {
                await notificationService.CreateAsync(new CreateNotificationRequest(
                    UserId: oldDentistUserId.Value,
                    Type: NotificationType.Appointment,
                    Priority: NotificationPriority.Medium,
                    Title: "Lịch hẹn đã chuyển sang bác sĩ khác",
                    Body: $"Lịch hẹn lúc {vnOld:HH:mm dd/MM/yyyy} đã được dời và chuyển sang bác sĩ khác.",
                    RelatedEntityType: "Appointment",
                    RelatedEntityId: appointment.Id.ToString()), ct);
            }
        }

        // Không tự gửi thông báo về cho chính người vừa bấm dời — họ vừa thấy màn hình xác nhận rồi.
        var patientUserId = await AppointmentStatusHelper.GetPatientUserIdAsync(patientRepository, appointment.PatientId, ct);
        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty && patientUserId.Value != currentUser.UserId)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn đã được dời",
                Body: $"Lịch khám đã được dời từ {vnOld:HH:mm dd/MM/yyyy} sang {vnNew:HH:mm dd/MM/yyyy}.{reasonSuffix}",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }
    }
}
