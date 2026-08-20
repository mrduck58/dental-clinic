using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Booking;

public class CreateAppointmentChangeRequestHandler(
    IAppointmentRepository appointmentRepository,
    IAppointmentChangeRequestRepository changeRequestRepository,
    IPatientRepository patientRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ILogger<CreateAppointmentChangeRequestHandler>? logger = null) : IRequestHandler<CreateAppointmentChangeRequestCommand, AppointmentChangeRequestDto>
{
    public async Task<AppointmentChangeRequestDto> Handle(CreateAppointmentChangeRequestCommand command, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var appointment = await appointmentRepository.GetByIdAsync(command.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        // Kiểm tra quyền sở hữu
        var primaryPatient = await patientRepository.GetByUserIdAsync(command.UserId, ct);
        var owns = primaryPatient != null && (appointment.PatientId == primaryPatient.Id ||
                   appointment.Patient?.PrimaryPatientId == primaryPatient.Id ||
                   appointment.Patient?.UserId == command.UserId);

        if (!owns)
        {
            throw new ForbiddenException("Bạn không có quyền gửi yêu cầu cho lịch hẹn này.");
        }

        // Kiểm tra trạng thái lịch hẹn
        if (appointment.Status != AppointmentStatus.Pending && appointment.Status != AppointmentStatus.Confirmed)
        {
            throw new ConflictException($"Không thể gửi yêu cầu thay đổi cho lịch hẹn có trạng thái {appointment.Status}.");
        }

        if (now >= appointment.AppointmentDate)
        {
            throw new ConflictException("Lịch hẹn đã đến hoặc đã qua giờ hẹn, không thể gửi yêu cầu thay đổi.");
        }

        // Kiểm tra nếu đã có yêu cầu Pending
        var existingPending = await changeRequestRepository.GetPendingByAppointmentIdAsync(appointment.Id, ct);
        if (existingPending != null)
        {
            throw new ConflictException("Lịch hẹn này đang có một yêu cầu chờ phòng khám xét duyệt. Vui lòng chờ phản hồi.");
        }

        AppointmentChangeRequest request;
        if (command.Type == AppointmentChangeType.Cancel)
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                throw new ValidationException("Vui lòng cung cấp lý do hủy lịch.");
            }

            request = AppointmentChangeRequest.CreateCancelRequest(
                appointmentId: appointment.Id,
                patientId: appointment.PatientId,
                requestedByUserId: command.UserId,
                reason: command.Reason.Trim());
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                throw new ValidationException("Vui lòng cung cấp lý do dời lịch.");
            }

            request = AppointmentChangeRequest.CreateRescheduleRequest(
                appointmentId: appointment.Id,
                patientId: appointment.PatientId,
                requestedByUserId: command.UserId,
                reason: command.Reason.Trim(),
                desiredDate: command.DesiredDate,
                desiredTimeSlot: command.DesiredTimeSlot,
                desiredDentistId: command.DesiredDentistId);
        }

        await changeRequestRepository.AddAsync(request, ct);
        logger?.LogInformation("Created change request {RequestId} for Appointment {AppointmentId} (Type: {Type})",
            request.Id, appointment.Id, request.Type);

        await activityLogService.LogAsync(
            userId: command.UserId,
            userName: "Bệnh nhân",
            userRole: "Patient",
            action: "Create",
            module: "Appointment",
            description: $"Gửi yêu cầu {(request.Type == AppointmentChangeType.Cancel ? "hủy" : "dời")} lịch hẹn ID: {appointment.Id}. Lý do: {request.Reason}",
            status: "Success",
            targetId: appointment.Id.ToString(),
            ct: ct);

        // Nạp lại đầy đủ quan hệ để map DTO
        var createdWithNav = await changeRequestRepository.GetByIdAsync(request.Id, ct) ?? request;
        return AppointmentChangeRequestDto.FromEntity(createdWithNav);
    }
}
