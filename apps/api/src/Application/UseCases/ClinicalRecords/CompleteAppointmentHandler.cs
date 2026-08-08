using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record CompleteAppointmentCommand(Guid AppointmentId) : IRequest;

/// <summary>Hoàn thành buổi khám — tách ra từ god-handler <c>UpdateAppointmentStatusHandler</c>.</summary>
public class CompleteAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser,
    ILogger<CompleteAppointmentHandler>? logger = null) : IRequestHandler<CompleteAppointmentCommand>
{
    public async Task Handle(CompleteAppointmentCommand command, CancellationToken ct)
    {
        var appointmentId = command.AppointmentId;

        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for Complete", appointmentId);
            throw new NotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        appointment.Complete();
        await appointmentRepository.UpdateAsync(appointment, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Hoàn thành lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);
    }
}
