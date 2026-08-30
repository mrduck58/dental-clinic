using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Application.UseCases.Patients;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record CreateWalkInCommand(
    Guid DentistId,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string PatientPhone,
    DateOnly DateOfBirth,
    string Gender,
    Guid? ServiceId,
    string? Symptoms,
    Guid? PatientId = null,
    string? PatientEmail = null,
    string? EmailVerificationCode = null,
    Guid? FollowUpFromAppointmentId = null,
    Guid? FollowUpId = null,
    AppointmentType AppointmentType = AppointmentType.GeneralExam,
    int DurationMinutes = 30) : IRequest<CreateWalkInResult>;

public record CreateWalkInResult(
    Guid AppointmentId,
    string AppointmentCode,
    string PatientName,
    string Status);

public class CreateWalkInAppointmentHandler(
    IAppointmentRepository appointmentRepository,
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    INotificationService notificationService,
    IFollowUpRepository? followUpRepository = null,
    ISender sender = null!)
    : IRequestHandler<CreateWalkInCommand, CreateWalkInResult>
{
    public async Task<CreateWalkInResult> Handle(CreateWalkInCommand cmd, CancellationToken ct)
    {
        var utcAppointmentDate = cmd.AppointmentDate.ToUniversalTime();

        // 1. Không cho đặt lịch cho khung giờ đã qua 15 phút
        if (utcAppointmentDate.AddMinutes(SlotCalculator.WalkInGraceMinutes) <= DateTimeOffset.UtcNow)
            throw new ValidationException("Không thể đặt lịch cho khung giờ đã qua.");

        // 1b. Kiểm tra bác sĩ có tồn tại không
        var dentistUserId = await appointmentRepository.GetDentistUserIdAsync(cmd.DentistId, ct);
        if (!dentistUserId.HasValue)
            throw new ValidationException($"Không tìm thấy bác sĩ với ID: '{cmd.DentistId}'.");

        // 1c. Check-in tái khám
        if (cmd.FollowUpFromAppointmentId is { } originalAppointmentId)
        {
            var original = await appointmentRepository.GetByIdAsync(originalAppointmentId, ct)
                ?? throw new NotFoundException("Không tìm thấy buổi hẹn gốc.");

            if (original.FollowUpDate == null && cmd.FollowUpId == null)
                throw new ValidationException("Buổi hẹn này chưa được bác sĩ hẹn tái khám.");

            var alreadyCheckedIn = await appointmentRepository.HasActiveFollowUpCheckInAsync(originalAppointmentId, ct);
            if (alreadyCheckedIn)
                throw new ConflictException("Buổi hẹn này đã được check-in tái khám.");
        }

        // 2. Kiểm tra slot còn trống
        var isBooked = await appointmentRepository.IsSlotBookedAsync(cmd.DentistId, utcAppointmentDate, ct);
        if (isBooked)
            throw new ConflictException("Khung giờ này đã được đặt. Vui lòng chọn giờ khác.");

        Patient? patient = null;
        var patientExplicitlySelected = cmd.PatientId is not null;

        if (cmd.PatientId is { } patientId)
        {
            patient = await patientRepository.GetByIdAsync(patientId, ct)
                ?? throw new ValidationException("Không tìm thấy bệnh nhân.");
        }
        else
        {
            var normalizedInputPhone = cmd.PatientPhone.Trim();
            var familyMembers = await patientRepository.GetFamilyByPhoneNumberAsync(normalizedInputPhone, ct);

            if (familyMembers.Count > 0)
            {
                var normalizedInputName = cmd.PatientName.Trim();
                var matchedByName = familyMembers.FirstOrDefault(p =>
                    string.Equals(p.FullName.Trim(), normalizedInputName, StringComparison.OrdinalIgnoreCase));

                if (matchedByName != null)
                {
                    patient = matchedByName;
                }
                else
                {
                    var primaryPatient = familyMembers.FirstOrDefault(p => p.PrimaryPatientId == null) ?? familyMembers[0];

                    var tempUser = User.CreatePatient(
                        fullName: cmd.PatientName.Trim(),
                        phoneNumber: normalizedInputPhone,
                        gender: cmd.Gender,
                        dateOfBirth: cmd.DateOfBirth);

                    patient = Patient.Create(
                        userId: tempUser.Id,
                        dateOfBirth: cmd.DateOfBirth,
                        primaryPatientId: primaryPatient.Id,
                        relationship: "Người thân");

                    patient.User = tempUser;
                    await patientRepository.AddAsync(patient, ct);
                }
            }
        }

        if (patient is null)
        {
            var tempUser = User.CreatePatient(
                fullName: cmd.PatientName,
                phoneNumber: cmd.PatientPhone,
                gender: cmd.Gender,
                dateOfBirth: cmd.DateOfBirth);

            patient = Patient.Create(
                userId: tempUser.Id,
                dateOfBirth: cmd.DateOfBirth);

            patient.User = tempUser;
            await patientRepository.AddAsync(patient, ct);
        }
        else
        {
            if (patientExplicitlySelected)
            {
                patient.SetDateOfBirth(cmd.DateOfBirth);
                patient.SetGender(cmd.Gender);
                patient.SetFullName(cmd.PatientName);
                patient.SetPhoneNumber(cmd.PatientPhone);

                await patientRepository.UpdateAsync(patient, ct);
            }
        }

        if (cmd.PatientEmail is not null && cmd.EmailVerificationCode is not null && (patient.User == null || (!patient.User.HasAccount && string.IsNullOrEmpty(patient.User.Email))))
        {
            await sender.Send(new CreatePatientAccountCommand(
                FullName: patient.FullName,
                Email: cmd.PatientEmail,
                PhoneNumber: patient.PhoneNumber ?? cmd.PatientPhone,
                DateOfBirth: patient.DateOfBirth,
                Gender: patient.Gender,
                VerificationCode: cmd.EmailVerificationCode), ct);
        }

        var apptType = (cmd.FollowUpFromAppointmentId.HasValue || cmd.FollowUpId.HasValue) ? AppointmentType.FollowUp : cmd.AppointmentType;
        var duration = cmd.DurationMinutes > 0 ? cmd.DurationMinutes : 30;

        var appointment = Appointment.CreateWalkIn(
            patientId: patient.Id,
            dentistId: cmd.DentistId,
            appointmentDate: utcAppointmentDate,
            symptoms: cmd.Symptoms,
            serviceId: cmd.ServiceId,
            followUpFromAppointmentId: cmd.FollowUpFromAppointmentId,
            appointmentType: apptType,
            durationMinutes: duration,
            followUpId: cmd.FollowUpId);

        await appointmentRepository.AddAsync(appointment, ct);

        if (cmd.FollowUpId.HasValue && followUpRepository != null)
        {
            var followUp = await followUpRepository.GetByIdAsync(cmd.FollowUpId.Value, ct);
            if (followUp != null)
            {
                followUp.LinkAppointment(appointment.Id);
                await followUpRepository.UpdateAsync(followUp, ct);
            }
        }

        var appointmentCode = ClinicalRecordMappers.AppointmentCode(appointment);

        var dateFormatted = cmd.AppointmentDate.ToString("HH:mm dd/MM/yyyy");
        await notificationService.CreateAsync(new CreateNotificationRequest(
            UserId: dentistUserId.Value,
            Type: NotificationType.Appointment,
            Priority: NotificationPriority.High,
            Title: "Bệnh nhân check-in tại quầy",
            Body: $"{cmd.PatientName} đã được check-in vào phòng khám lúc {dateFormatted}.",
            RelatedEntityType: "Appointment",
            RelatedEntityId: appointment.Id.ToString()), ct);

        return new CreateWalkInResult(
            AppointmentId: appointment.Id,
            AppointmentCode: appointmentCode,
            PatientName: cmd.PatientName,
            Status: appointment.Status.ToString());
    }
}
