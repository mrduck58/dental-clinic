using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateAppointmentCommand(
    Guid UserId,
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string? Symptoms,
    Guid? ServiceId,
    Guid? PatientId);

public record CreateAppointmentResult(
    Guid AppointmentId,
    string AppointmentCode,
    string Status);

public class CreateAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    INotificationService notificationService)
{
    public async Task<CreateAppointmentResult> HandleAsync(CreateAppointmentCommand cmd, CancellationToken ct = default)
    {
        var primaryPatient = await patientRepository.GetByUserIdAsync(cmd.UserId, ct);

        if (primaryPatient is null)
        {
            var user = await userRepository.GetByIdAsync(cmd.UserId, ct)
                ?? throw new InvalidOperationException("Không tìm thấy tài khoản.");

            primaryPatient = Patient.Create(
                user.FullName ?? user.Email,
                user.DateOfBirth ?? new DateOnly(1990, 1, 1),
                user.Gender ?? "Nam",
                cmd.UserId);

            await patientRepository.AddAsync(primaryPatient, ct);
        }

        var targetPatientId = primaryPatient.Id;
        if (cmd.PatientId.HasValue && cmd.PatientId.Value != primaryPatient.Id)
        {
            var member = await patientRepository.GetByIdAsync(cmd.PatientId.Value, ct);
            if (member == null || member.PrimaryPatientId != primaryPatient.Id)
            {
                throw new InvalidOperationException("Hồ sơ bệnh nhân không hợp lệ hoặc không thuộc gia đình bạn.");
            }
            targetPatientId = member.Id;
        }

        var alreadyBooked = await appointmentRepository.IsSlotBookedAsync(cmd.DentistId, cmd.AppointmentDate, ct);
        if (alreadyBooked)
            throw new ConflictException("Khung giờ này đã được đặt. Vui lòng chọn giờ khác.");

        var appointment = Appointment.Create(
            targetPatientId,
            cmd.DentistId,
            cmd.AppointmentDate,
            symptoms: cmd.Symptoms,
            serviceId: cmd.ServiceId);

        await appointmentRepository.AddAsync(appointment, ct);

        var code = $"DK{cmd.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}";

        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(cmd.DentistId, ct);
        if (dentistUserId.HasValue)
        {
            await notificationService.CreateAsync(new CreateNotificationRequest(
                UserId: dentistUserId.Value,
                Type: NotificationType.Appointment,
                Priority: NotificationPriority.High,
                Title: "Lịch hẹn mới",
                Body: $"Bạn có lịch hẹn mới vào {cmd.AppointmentDate:dd/MM/yyyy HH:mm}.",
                RelatedEntityType: "Appointment",
                RelatedEntityId: appointment.Id.ToString()), ct);
        }

        var staffIds = await userRepository.GetUserIdsByRoleAsync("Staff", ct);
        var staffTemplate = new CreateNotificationRequest(
            UserId: Guid.Empty,
            Type: NotificationType.Appointment,
            Priority: NotificationPriority.High,
            Title: "Đặt lịch mới",
            Body: $"Có lịch hẹn mới vào {cmd.AppointmentDate:dd/MM/yyyy HH:mm}. Vui lòng xác nhận.",
            RelatedEntityType: "Appointment",
            RelatedEntityId: appointment.Id.ToString());
        await notificationService.CreateForMultipleUsersAsync(staffIds, staffTemplate, ct);

        return new CreateAppointmentResult(appointment.Id, code, appointment.Status.ToString());
    }
}
