using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Appointments;

public class UpdateAppointmentStatusHandler(
    IAppointmentRepository appointmentRepository,
    IActivityLogService activityLogService,
    INotificationService notificationService,
    ICurrentUserService currentUser,
    IPatientRepository? patientRepository = null,
    ILogger<UpdateAppointmentStatusHandler>? logger = null)
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private async Task<Guid?> GetPatientUserIdAsync(Guid patientId, CancellationToken ct)
    {
        if (patientRepository == null) return null;
        var patient = await patientRepository.GetByIdAsync(patientId, ct);
        if (patient == null) return null;
        if (patient.PrimaryPatientId.HasValue)
        {
            var primary = await patientRepository.GetByIdAsync(patient.PrimaryPatientId.Value, ct);
            return primary?.UserId ?? patient.UserId;
        }
        return patient.UserId;
    }

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

        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, VietnamTz);

        // 1. Gửi thông báo cho bác sĩ
        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.Medium,
                Title: "Lịch hẹn đã xác nhận",
                Body: $"Lịch hẹn vào {vnDate:HH:mm dd/MM/yyyy} đã được xác nhận.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }

        // 2. Gửi thông báo cho bệnh nhân (hoặc chủ tài khoản gia đình)
        var patientUserId = await GetPatientUserIdAsync(appointment.PatientId, ct);
        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn đã được xác nhận!",
                Body: $"Lịch hẹn khám nha khoa của bạn vào lúc {vnDate:HH:mm} ngày {vnDate:dd/MM/yyyy} đã được phòng khám xác nhận. Vui lòng đến đúng giờ!",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
    }

    public async Task CancelAsync(Guid appointmentId, string? reason = null, CancellationToken ct = default)
    {
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

        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, VietnamTz);

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

        var patientUserId = await GetPatientUserIdAsync(appointment.PatientId, ct);
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

        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, VietnamTz);

        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Bệnh nhân đã check-in",
                Body: $"Bệnh nhân đã check-in lịch hẹn vào {vnDate:HH:mm dd/MM/yyyy}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }

        var patientUserId = await GetPatientUserIdAsync(appointment.PatientId, ct);
        if (patientUserId.HasValue && patientUserId.Value != Guid.Empty)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: patientUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.Medium,
                Title: "Check-in thành công",
                Body: $"Bạn đã check-in thành công cho lịch hẹn lúc {vnDate:HH:mm} ngày {vnDate:dd/MM/yyyy}. Vui lòng chờ đến lượt vào khám.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
    }

    public async Task MarkNoShowAsync(Guid appointmentId, CancellationToken ct = default)
    {
        var appointment = await appointmentRepository.GetByIdAsync(appointmentId, ct);
        if (appointment == null)
        {
            logger?.LogWarning("Appointment {Id} not found for MarkNoShow", appointmentId);
            throw new KeyNotFoundException($"Không tìm thấy lịch hẹn {appointmentId}.");
        }

        // Chỉ ghi nhận vắng với lịch đã xác nhận nhưng chưa check-in — đúng bối cảnh
        // lễ tân dùng ở quầy check-in.
        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            logger?.LogWarning("MarkNoShow failed for {Id}: status is {Status}, expected Confirmed", appointmentId, appointment.Status);
            throw new InvalidOperationException($"Chỉ có thể ghi nhận vắng với lịch hẹn đã xác nhận. Trạng thái hiện tại: {appointment.Status}");
        }

        appointment.MarkNoShow();
        await appointmentRepository.UpdateAsync(appointment, ct);
        logger?.LogInformation("Appointment {Id} marked as no-show", appointmentId);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Edit,
            module: ActivityModule.Appointment,
            description: $"Ghi nhận vắng mặt lịch hẹn ID: {appointmentId}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: appointmentId.ToString(),
            ct: ct);

        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(appointment.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.Medium,
                Title: "Bệnh nhân vắng mặt",
                Body: $"Bệnh nhân không đến khám lịch hẹn vào {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointmentId.ToString()), ct);
        }
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
            throw new ValidationException("Chỉ có thể bắt đầu khám lịch hẹn đã check-in.");

        // Mỗi bác sĩ chỉ khám một bệnh nhân tại một thời điểm — chỉ xét trong cùng ngày với buổi hẹn này,
        // để một ca cũ quên bấm "Kết thúc điều trị" không khóa bác sĩ ở những ngày sau.
        var vnDate = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, VietnamTz);
        var dayStartUtc = new DateTimeOffset(vnDate.Year, vnDate.Month, vnDate.Day, 0, 0, 0, VietnamTz.BaseUtcOffset).ToUniversalTime();
        var inProgress = await appointmentRepository.GetInProgressByDentistAsync(
            appointment.DentistId, appointmentId, dayStartUtc, dayStartUtc.AddDays(1), ct);
        if (inProgress != null)
            throw new ConflictException(
                $"Bạn đang khám bệnh nhân {inProgress.Patient.FullName}. Vui lòng kết thúc điều trị bệnh nhân đó trước khi bắt đầu khám bệnh nhân mới.");

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

        // Bác sĩ có hẹn tái khám → báo cho bệnh nhân (nếu có tài khoản liên kết).
        if (appointment.FollowUpDate is DateOnly followUpDate && patientRepository != null)
        {
            var patient = await patientRepository.GetByIdAsync(appointment.PatientId, ct);
            if (patient?.UserId is Guid patientUserId)
            {
                var noteSuffix = string.IsNullOrWhiteSpace(appointment.FollowUpNote) ? "" : $" Ghi chú: {appointment.FollowUpNote}";
                await notificationService.CreateAsync(new CreateNotificationRequest(
                    UserId: patientUserId,
                    Type: NotificationType.Reminder,
                    Priority: NotificationPriority.Medium,
                    Title: "Lịch tái khám",
                    Body: $"Bác sĩ hẹn bạn tái khám vào ngày {followUpDate:dd/MM/yyyy}. Vui lòng đặt lịch trước ngày hẹn.{noteSuffix}",
                    RelatedEntityType: "Appointment",
                    RelatedEntityId: appointmentId.ToString()), ct);
            }
        }
    }
}
