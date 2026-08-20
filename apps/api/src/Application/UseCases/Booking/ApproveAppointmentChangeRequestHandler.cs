using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

public class ApproveAppointmentChangeRequestHandler(
    IAppointmentChangeRequestRepository changeRequestRepository,
    IAppointmentRepository appointmentRepository,
    INotificationService notificationService,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser,
    ILogger<ApproveAppointmentChangeRequestHandler>? logger = null) : IRequestHandler<ApproveAppointmentChangeRequestCommand>
{
    public async Task Handle(ApproveAppointmentChangeRequestCommand command, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var request = await changeRequestRepository.GetByIdAsync(command.RequestId, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu thay đổi lịch hẹn.");

        if (request.Status != AppointmentChangeRequestStatus.Pending)
        {
            throw new ConflictException($"Yêu cầu này đã được xử lý (trạng thái: {request.Status}).");
        }

        var appointment = request.Appointment
            ?? await appointmentRepository.GetByIdAsync(request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn liên quan.");

        var code = $"DK{appointment.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}";

        if (request.Type == AppointmentChangeType.Cancel)
        {
            // Bệnh nhân yêu cầu hủy và được lễ tân duyệt -> Hủy lịch
            appointment.Cancel(CancellationReason.PatientRequested, request.Reason, command.StaffUserId, now);
            await appointmentRepository.UpdateAsync(appointment, ct);

            request.Approve(command.StaffUserId, command.StaffNote);
            await changeRequestRepository.UpdateAsync(request, ct);

            logger?.LogInformation("Staff {StaffId} approved cancel request {RequestId} for Appointment {ApptId}",
                command.StaffUserId, request.Id, appointment.Id);

            // Gửi thông báo cho bệnh nhân
            var patientUserId = appointment.Patient?.UserId ?? appointment.Patient?.PrimaryPatient?.UserId;
            if (patientUserId.HasValue)
            {
                await notificationService.CreateAsync(new CreateNotificationRequest(
                    UserId: patientUserId.Value,
                    Type: NotificationType.Appointment,
                    Priority: NotificationPriority.High,
                    Title: "Yêu cầu hủy lịch đã được chấp nhận",
                    Body: $"Phòng khám đã xác nhận hủy lịch hẹn #{code} theo yêu cầu của bạn.",
                    RelatedEntityType: "Appointment",
                    RelatedEntityId: appointment.Id.ToString()), ct);
            }
        }
        else if (request.Type == AppointmentChangeType.Reschedule)
        {
            if (request.DesiredDate.HasValue)
            {
                var targetDentistId = request.DesiredDentistId ?? appointment.DentistId;
                var targetDate = request.DesiredDate.Value;

                appointment.Reschedule(targetDate, targetDentistId, appointment.ServiceId, requiresReconfirmation: false, now);
                await appointmentRepository.UpdateAsync(appointment, ct);

                request.Approve(command.StaffUserId, command.StaffNote);
                await changeRequestRepository.UpdateAsync(request, ct);

                var vnDate = TimeZoneInfo.ConvertTime(targetDate, AppointmentStatusHelper.VietnamTz);
                var patientUserId = appointment.Patient?.UserId ?? appointment.Patient?.PrimaryPatient?.UserId;
                if (patientUserId.HasValue)
                {
                    await notificationService.CreateAsync(new CreateNotificationRequest(
                        UserId: patientUserId.Value,
                        Type: NotificationType.Appointment,
                        Priority: NotificationPriority.High,
                        Title: "Yêu cầu dời lịch đã được chấp nhận",
                        Body: $"Lịch hẹn #{code} của bạn đã được dời sang {vnDate:HH:mm dd/MM/yyyy}.",
                        RelatedEntityType: "Appointment",
                        RelatedEntityId: appointment.Id.ToString()), ct);
                }
            }
            else
            {
                // Mở khóa cho bệnh nhân tự vào chọn ngày và ca khám mới trong vòng 48 giờ
                appointment.UnlockSelfManagement(now.AddHours(48));
                await appointmentRepository.UpdateAsync(appointment, ct);

                request.Approve(command.StaffUserId, command.StaffNote);
                await changeRequestRepository.UpdateAsync(request, ct);

                var patientUserId = appointment.Patient?.UserId ?? appointment.Patient?.PrimaryPatient?.UserId;
                if (patientUserId.HasValue)
                {
                    await notificationService.CreateAsync(new CreateNotificationRequest(
                        UserId: patientUserId.Value,
                        Type: NotificationType.Appointment,
                        Priority: NotificationPriority.High,
                        Title: "Yêu cầu dời lịch đã được chấp thuận",
                        Body: $"Phòng khám đã duyệt yêu cầu dời lịch hẹn #{code}. Bạn có thể vào chi tiết lịch hẹn trên ứng dụng để tự chọn ngày và ca khám mới.",
                        RelatedEntityType: "Appointment",
                        RelatedEntityId: appointment.Id.ToString()), ct);
                }
            }

            logger?.LogInformation("Staff {StaffId} approved reschedule request {RequestId} for Appointment {ApptId}",
                command.StaffUserId, request.Id, appointment.Id);
        }

        await activityLogService.LogAsync(
            userId: command.StaffUserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: "Approve",
            module: "Appointment",
            description: $"Duyệt yêu cầu {(request.Type == AppointmentChangeType.Cancel ? "hủy" : "dời")} lịch hẹn ID: {appointment.Id}",
            status: "Success",
            targetId: appointment.Id.ToString(),
            ct: ct);
    }
}
