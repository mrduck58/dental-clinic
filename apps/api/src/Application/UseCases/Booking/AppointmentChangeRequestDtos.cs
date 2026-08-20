using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Application.UseCases.Booking;

public record CreateAppointmentChangeRequestCommand(
    Guid AppointmentId,
    Guid UserId,
    AppointmentChangeType Type,
    string Reason,
    DateTimeOffset? DesiredDate = null,
    string? DesiredTimeSlot = null,
    Guid? DesiredDentistId = null) : MediatR.IRequest<AppointmentChangeRequestDto>;

public record GetStaffAppointmentChangeRequestsQuery(
    AppointmentChangeRequestStatus? Status = null,
    DateOnly? Date = null) : MediatR.IRequest<IReadOnlyList<AppointmentChangeRequestDto>>;

public record ApproveAppointmentChangeRequestCommand(
    Guid RequestId,
    Guid StaffUserId,
    string? StaffNote = null) : MediatR.IRequest;

public record RejectAppointmentChangeRequestCommand(
    Guid RequestId,
    Guid StaffUserId,
    string StaffNote) : MediatR.IRequest;

public record AppointmentChangeRequestDto(
    Guid Id,
    Guid AppointmentId,
    string AppointmentCode,
    Guid PatientId,
    string PatientName,
    string? PatientPhone,
    string? ServiceName,
    DateTimeOffset CurrentAppointmentDate,
    string CurrentDentistName,
    AppointmentChangeType Type,
    AppointmentChangeRequestStatus Status,
    string Reason,
    DateTimeOffset? DesiredDate,
    string? DesiredTimeSlot,
    Guid? DesiredDentistId,
    string? DesiredDentistName,
    string? StaffNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    string? ProcessedByName)
{
    public static AppointmentChangeRequestDto FromEntity(AppointmentChangeRequest r)
    {
        var appt = r.Appointment;
        var patient = appt?.Patient;
        var dentist = appt?.Dentist;
        var service = appt?.Service;

        var patientName = patient?.User?.FullName ?? patient?.FullName ?? string.Empty;
        var patientPhone = patient?.User?.PhoneNumber ?? patient?.PhoneNumber;
        var currentDentistName = dentist?.Employee?.User?.FullName ?? "Bác sĩ";
        var desiredDentistName = r.DesiredDentist?.Employee?.User?.FullName;
        var processedByName = r.ProcessedByUser?.FullName;

        return new AppointmentChangeRequestDto(
            Id: r.Id,
            AppointmentId: r.AppointmentId,
            AppointmentCode: appt != null ? $"DK{appt.AppointmentDate:yyyyMMdd}{appt.Id.ToString("N")[..6].ToUpper()}" : string.Empty,
            PatientId: r.PatientId,
            PatientName: patientName,
            PatientPhone: patientPhone,
            ServiceName: service?.Name,
            CurrentAppointmentDate: appt?.AppointmentDate ?? DateTimeOffset.MinValue,
            CurrentDentistName: currentDentistName,
            Type: r.Type,
            Status: r.Status,
            Reason: r.Reason,
            DesiredDate: r.DesiredDate,
            DesiredTimeSlot: r.DesiredTimeSlot,
            DesiredDentistId: r.DesiredDentistId,
            DesiredDentistName: desiredDentistName,
            StaffNote: r.StaffNote,
            CreatedAt: r.CreatedAt,
            ProcessedAt: r.ProcessedAt,
            ProcessedByName: processedByName);
    }
}
