using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record CreateAppointmentCommand(
    Guid UserId,
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string? Symptoms,
    Guid? ServiceId,
    Guid? PatientId) : IRequest<CreateAppointmentResult>;

public record CreateAppointmentResult(
    Guid AppointmentId,
    string AppointmentCode,
    string Status);

public class CreateAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    AppointmentSlotGuard slotGuard,
    INotificationService notificationService) : IRequestHandler<CreateAppointmentCommand, CreateAppointmentResult>
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

        await slotGuard.EnsureSlotAvailableAsync(
            cmd.DentistId, utcAppointmentDate, cmd.ServiceId, excludeAppointmentId: null, ct);

        var appointment = Appointment.Create(
            targetPatientId,
            cmd.DentistId,
            utcAppointmentDate,
            symptoms: cmd.Symptoms,
            serviceId: cmd.ServiceId);

        await appointmentRepository.AddAsync(appointment, ct);

        var code = $"DK{cmd.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}";

        var vnTime = cmd.AppointmentDate.UtcDateTime.AddHours(7);

        // Thông báo cho tài khoản bệnh nhân đặt lịch
        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: cmd.UserId,
            Type: NotificationType.Appointment,
            Priority: NotificationPriority.Medium,
            Title: "Đặt lịch hẹn thành công",
            Body: $"Lịch khám của bạn vào {vnTime:HH:mm dd/MM/yyyy} đã được ghi nhận. Vui lòng chờ phòng khám xác nhận.",
            RelatedEntityType: "Appointment",
            RelatedEntityId: appointment.Id.ToString()), ct);

        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn mới",
                Body: $"Bạn có lịch hẹn mới vào {vnTime:HH:mm dd/MM/yyyy}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }

        var staffIds = await userRepository.GetUserIdsByRoleAsync("Staff", ct);
        var staffTemplate = new CreateNotificationRequest(
            UserId: Guid.Empty,
            Type: NotificationType.Appointment,
            Priority: NotificationPriority.High,
            Title: "Đặt lịch mới",
            Body: $"Có lịch hẹn mới vào {vnTime:HH:mm dd/MM/yyyy}. Vui lòng xác nhận.",
            RelatedEntityType: "Appointment",
            RelatedEntityId: appointment.Id.ToString());
        await notificationService.CreateForMultipleUsersAsync(staffIds, staffTemplate, ct);

        return new CreateAppointmentResult(appointment.Id, code, appointment.Status.ToString());
    }
}
