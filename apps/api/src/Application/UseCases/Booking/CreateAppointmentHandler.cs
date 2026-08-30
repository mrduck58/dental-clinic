using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record CreateAppointmentCommand(
    Guid UserId,
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string? Symptoms,
    Guid? ServiceId,
    Guid? PatientId,
    AppointmentType AppointmentType = AppointmentType.GeneralExam,
    int DurationMinutes = 30,
    Guid? FollowUpId = null,
    List<Guid>? TreatmentSessionIds = null) : IRequest<CreateAppointmentResult>;

public record CreateAppointmentResult(
    Guid AppointmentId,
    string AppointmentCode,
    string Status);

public class CreateAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    AppointmentSlotGuard slotGuard,
    INotificationService notificationService,
    IFollowUpRepository? followUpRepository = null,
    ITreatmentPlanRepository? treatmentPlanRepository = null,
    ISlotHoldRepository? slotHoldRepository = null,
    ISlotNotifier? slotNotifier = null,
    IServiceRepository? serviceRepository = null) : IRequestHandler<CreateAppointmentCommand, CreateAppointmentResult>
{
    public async Task<CreateAppointmentResult> Handle(CreateAppointmentCommand cmd, CancellationToken ct)
    {
        var utcAppointmentDate = cmd.AppointmentDate.ToUniversalTime();

        var primaryPatient = await patientRepository.GetByUserIdAsync(cmd.UserId, ct);

        if (primaryPatient is null)
        {
            var user = await userRepository.GetByIdAsync(cmd.UserId, ct)
                ?? throw new ValidationException("Không tìm thấy tài khoản.");

            primaryPatient = Patient.Create(
                userId: cmd.UserId,
                dateOfBirth: null);
            primaryPatient.User = user;

            await patientRepository.AddAsync(primaryPatient, ct);
        }

        var targetPatientId = primaryPatient.Id;
        if (cmd.PatientId.HasValue && cmd.PatientId.Value != primaryPatient.Id)
        {
            var member = await patientRepository.GetByIdAsync(cmd.PatientId.Value, ct);
            if (member == null || member.PrimaryPatientId != primaryPatient.Id)
            {
                throw new ValidationException("Hồ sơ bệnh nhân không hợp lệ hoặc không thuộc gia đình bạn.");
            }
            targetPatientId = member.Id;
        }

        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(cmd.DentistId, ct);
        if (!dentistUserId.HasValue)
            throw new ValidationException($"Không tìm thấy bác sĩ với ID: '{cmd.DentistId}'.");

        var now = DateTimeOffset.UtcNow;

        // 1. Kiểm tra nếu bệnh nhân đang trong thời gian chờ (cooldown 30 phút)
        var cooldownUntil = await appointmentRepository.GetPatientCooldownUntilAsync(targetPatientId, now, ct);
        if (cooldownUntil.HasValue && cooldownUntil.Value > now)
        {
            var remaining = (int)Math.Ceiling((cooldownUntil.Value - now).TotalMinutes);
            throw new ConflictException($"Bệnh nhân đang trong thời gian chờ sau khi hủy lịch. Vui lòng thử lại sau {remaining} phút.");
        }

        // 2. Kiểm tra mỗi bệnh nhân chỉ được có tối đa 1 lịch hẹn đang hoạt động
        var hasActiveAppointmentForPatient = await appointmentRepository.HasActiveAppointmentForPatientAsync(targetPatientId, excludeAppointmentId: null, ct);
        if (hasActiveAppointmentForPatient)
        {
            throw new ConflictException("Bệnh nhân này đã có một lịch hẹn đang hoạt động. Vui lòng hoàn thành hoặc dời/hủy lịch hẹn hiện tại trước khi đặt lịch mới.");
        }

        var localDate = DateOnly.FromDateTime(cmd.AppointmentDate.UtcDateTime.AddHours(7));
        var hasActiveAppointment = await appointmentRepository.HasActiveAppointmentOnDateAsync(
            targetPatientId, localDate, excludeAppointmentId: null, ct);
        if (hasActiveAppointment)
        {
            throw new ConflictException("Bệnh nhân này đã có một lịch hẹn trong ngày này. Mỗi bệnh nhân chỉ được đặt tối đa 1 lịch hẹn mỗi ngày (nếu muốn đổi giờ, vui lòng dời hoặc hủy lịch cũ).");
        }

        await slotGuard.EnsureSlotAvailableAsync(
            cmd.DentistId, utcAppointmentDate, cmd.ServiceId, excludeAppointmentId: null, ct);

        var duration = cmd.DurationMinutes > 0 ? cmd.DurationMinutes : 30;
        var apptType = cmd.FollowUpId.HasValue ? AppointmentType.FollowUp : cmd.AppointmentType;

        var appointment = Appointment.Create(
            targetPatientId,
            cmd.DentistId,
            utcAppointmentDate,
            symptoms: cmd.Symptoms,
            serviceId: cmd.ServiceId,
            appointmentType: apptType,
            durationMinutes: duration,
            followUpId: cmd.FollowUpId);

        // Gắn danh sách session nếu có
        if (cmd.TreatmentSessionIds != null && cmd.TreatmentSessionIds.Count > 0 && treatmentPlanRepository != null)
        {
            int seq = 1;
            foreach (var sId in cmd.TreatmentSessionIds)
            {
                var session = await treatmentPlanRepository.GetSessionByIdAsync(sId, ct);
                if (session != null)
                {
                    var apptSession = AppointmentSession.Create(appointment.Id, session.Id, seq++, session.DurationMinutes);
                    appointment.AppointmentSessions.Add(apptSession);
                }
            }
            var totalDuration = appointment.AppointmentSessions.Sum(s => s.DurationMinutes);
            if (totalDuration > 0) appointment.SetDuration(totalDuration);
        }

        await appointmentRepository.AddAsync(appointment, ct);

        // Liên kết FollowUp nếu có
        if (cmd.FollowUpId.HasValue && followUpRepository != null)
        {
            var followUp = await followUpRepository.GetByIdAsync(cmd.FollowUpId.Value, ct);
            if (followUp != null)
            {
                followUp.LinkAppointment(appointment.Id);
                await followUpRepository.UpdateAsync(followUp, ct);
            }
        }

        // 3. Xác nhận Hold thành công
        if (slotHoldRepository != null)
        {
            var hold = await slotHoldRepository.GetActiveHoldForSlotAsync(cmd.DentistId, utcAppointmentDate, now, ct);
            if (hold != null && hold.PatientId == targetPatientId)
            {
                hold.Confirm();
                await slotHoldRepository.UpdateAsync(hold, ct);
            }
        }

        // 4. Phát sự kiện realtime SlotBooked
        if (slotNotifier != null)
        {
            var vnTime = cmd.AppointmentDate.UtcDateTime.AddHours(7);
            var slotRange = $"{vnTime.Hour:D2}:{vnTime.Minute:D2} - {(vnTime.Hour + 1):D2}:{vnTime.Minute:D2}";
            await slotNotifier.NotifySlotBookedAsync(cmd.DentistId, localDate, slotRange, ct);
        }

        var appointmentCode = ClinicalRecordMappers.AppointmentCode(appointment);

        // 5. Gửi thông báo
        var dateFormatted = cmd.AppointmentDate.ToString("HH:mm dd/MM/yyyy");
        string? serviceName = null;
        if (cmd.ServiceId.HasValue && serviceRepository != null)
        {
            var service = await serviceRepository.GetByIdAsync(cmd.ServiceId.Value, ct);
            serviceName = service?.Name;
        }

        var notifBody = serviceName != null
            ? $"Lịch hẹn cho dịch vụ {serviceName} vào lúc {dateFormatted} đang chờ xác nhận."
            : $"Lịch hẹn vào lúc {dateFormatted} đang chờ xác nhận.";

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: cmd.UserId,
            Type: NotificationType.Appointment,
            Priority: NotificationPriority.High,
            Title: "Đặt lịch hẹn thành công",
            Body: notifBody,
            RelatedEntityType: "Appointment",
            RelatedEntityId: appointment.Id.ToString()), ct);

        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: dentistUserId.Value,
            Type: NotificationType.Appointment,
            Priority: NotificationPriority.High,
            Title: "Lịch hẹn mới",
            Body: $"Bệnh nhân đã đặt lịch hẹn mới vào lúc {dateFormatted}.",
            RelatedEntityType: "Appointment",
            RelatedEntityId: appointment.Id.ToString()), ct);

        var staffIds = await userRepository.GetUserIdsByRoleAsync("Staff", ct);
        if (staffIds.Count > 0)
        {
            await notificationService.CreateForMultipleUsersAsync(
                staffIds,
                new CreateNotificationRequest(
                    UserId: Guid.Empty,
                    Type: NotificationType.Appointment,
                    Priority: NotificationPriority.High,
                    Title: "Lịch hẹn mới",
                    Body: $"Bệnh nhân đã đặt lịch hẹn mới vào lúc {dateFormatted}.",
                    RelatedEntityType: "Appointment",
                    RelatedEntityId: appointment.Id.ToString()), ct);
        }

        return new CreateAppointmentResult(
            AppointmentId: appointment.Id,
            AppointmentCode: appointmentCode,
            Status: appointment.Status.ToString());
    }
}
