using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

public class RejectAppointmentChangeRequestHandler(
    IAppointmentChangeRequestRepository changeRequestRepository,
    IAppointmentRepository appointmentRepository,
    INotificationService notificationService,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser,
    ILogger<RejectAppointmentChangeRequestHandler>? logger = null) : IRequestHandler<RejectAppointmentChangeRequestCommand>
{
    public async Task Handle(RejectAppointmentChangeRequestCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.StaffNote))
        {
            throw new ValidationException("Vui lòng cung cấp lý do từ chối yêu cầu.");
        }

        var request = await changeRequestRepository.GetByIdAsync(command.RequestId, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu thay đổi lịch hẹn.");

        if (request.Status != AppointmentChangeRequestStatus.Pending)
        {
            throw new ConflictException($"Yêu cầu này đã được xử lý (trạng thái: {request.Status}).");
        }

        var appointment = request.Appointment
            ?? await appointmentRepository.GetByIdAsync(request.AppointmentId, ct);

        request.Reject(command.StaffUserId, command.StaffNote.Trim());
        await changeRequestRepository.UpdateAsync(request, ct);

        logger?.LogInformation("Staff {StaffId} rejected change request {RequestId} for Appointment {ApptId}",
            command.StaffUserId, request.Id, request.AppointmentId);

        var code = appointment != null ? $"DK{appointment.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}" : request.AppointmentId.ToString();

        // Gửi thông báo cho bệnh nhân
        var patientUserId = appointment?.Patient?.UserId ?? appointment?.Patient?.PrimaryPatient?.UserId;
        if (patientUserId.HasValue)
        {
            var actionText = request.Type == AppointmentChangeType.Cancel ? "hủy lịch" : "dời lịch";
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: $"Yêu cầu {actionText} không được chấp thuận",
                Body: $"Yêu cầu {actionText} cho lịch hẹn #{code} không thể thực hiện. Lý do: {command.StaffNote.Trim()}",
                RelatedEntityType: "Appointment",
                RelatedEntityId: request.AppointmentId.ToString()), ct);
        }

        await activityLogService.LogAsync(
            userId: command.StaffUserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: "Reject",
            module: "Appointment",
            description: $"Từ chối yêu cầu {(request.Type == AppointmentChangeType.Cancel ? "hủy" : "dời")} lịch hẹn ID: {request.AppointmentId}. Lý do: {command.StaffNote.Trim()}",
            status: "Success",
            targetId: request.AppointmentId.ToString(),
            ct: ct);
    }
}
