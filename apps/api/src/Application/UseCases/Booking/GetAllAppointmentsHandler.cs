using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Booking;

public record StaffAppointmentDto(
    Guid AppointmentId,
    string AppointmentCode,
    Guid PatientId,
    string PatientName,
    string? PatientPhone,
    string DentistName,
    string? ServiceName,
    DateTimeOffset AppointmentDate,
    DateTimeOffset CreatedAt,
    string Status,
    string? Symptoms,
    DateTimeOffset? CheckedInAt);

public record GetAllAppointmentsQuery(DateOnly? Date, string? Status)
    : IRequest<IEnumerable<StaffAppointmentDto>>;

public class GetAllAppointmentsHandler(IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAllAppointmentsQuery, IEnumerable<StaffAppointmentDto>>
{
    public async Task<IEnumerable<StaffAppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken ct)
    {
        var date = request.Date;
        var status = request.Status;

        AppointmentStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        var appointments = await appointmentRepository.GetStaffAppointmentsAsync(date, statusEnum, ct);

        return appointments.Select(a => new StaffAppointmentDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString()[..6].ToUpper()}",
            a.PatientId,
            a.Patient.FullName,
            a.Patient.User?.PhoneNumber,
            a.Dentist.FullName,
            a.Service?.Name,
            a.AppointmentDate,
            a.CreatedAt,
            a.Status.ToString(),
            a.Symptoms,
            a.CheckedInAt));
    }
}
