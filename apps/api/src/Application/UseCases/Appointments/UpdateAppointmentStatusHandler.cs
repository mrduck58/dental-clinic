using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Appointments;

public class UpdateAppointmentStatusHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    ICurrentUserService currentUser,
    ILogger<UpdateAppointmentStatusHandler>? logger = null)
{
    public async Task ConfirmAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for Confirm", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
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
    }

    public async Task CancelAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for Cancel", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }
        appointment.Cancel();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} cancelled", appointmentId);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Cancel,
            module: ActivityModule.Appointment,
            description: $"Hủy lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);
    }

    public async Task CheckInAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for CheckIn", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        logger?.LogInformation("CheckIn attempt for {Id}: current status = {Status}", appointmentId, appointment.Status);

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            logger?.LogWarning("CheckIn failed for {Id}: status is {Status}, expected Confirmed", appointmentId, appointment.Status);
            throw new InvalidOperationException($"Chỉ có thể check-in lịch hẹn đã được xác nhận. Trạng thái hiện tại: {appointment.Status}");
        }

        appointment.CheckIn();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} checked in successfully", appointmentId);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Check-in lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);
    }

    public async Task StartTreatmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for StartTreatment", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        if (appointment.Status != AppointmentStatus.CheckedIn)
            throw new InvalidOperationException("Chỉ có thể bắt đầu khám lịch hẹn đã check-in.");

        appointment.StartTreatment();
        await appointmentRepository.UpdateAsync(appointment, ct);
    }

    public async Task CompleteAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for Complete", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
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

    public async Task EndTreatmentAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for EndTreatment", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        if (appointment.Status != AppointmentStatus.InProgress)
            throw new InvalidOperationException("Chỉ có thể kết thúc điều trị khi đang trong trạng thái đang khám.");

        appointment.EndTreatment();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} ended treatment, moved to pending payment", appointmentId);
    }
}
