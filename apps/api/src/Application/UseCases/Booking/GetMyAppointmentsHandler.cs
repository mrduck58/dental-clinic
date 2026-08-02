using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Booking;

public record MyAppointmentDto(
    Guid AppointmentId,
    string AppointmentCode,
    string DentistName,
    string? DentistAvatarUrl,
    string Specialization,
    DateTimeOffset AppointmentDate,
    string Status,
    string? Symptoms,
    string? ServiceName,
    string PatientName,
    string? PatientRelationship,
    Guid PatientId);

public record GetMyAppointmentsQuery(Guid UserId) : IRequest<IEnumerable<MyAppointmentDto>>;

public class GetMyAppointmentsHandler(
    IPatientRepository patientRepository,
    AppDbContext dbContext) : IRequestHandler<GetMyAppointmentsQuery, IEnumerable<MyAppointmentDto>>
{
    public async Task<IEnumerable<MyAppointmentDto>> Handle(GetMyAppointmentsQuery request, CancellationToken ct)
    {
        var patient = await patientRepository.GetByUserIdAsync(request.UserId, ct);
        if (patient is null) return [];

        var appointments = await dbContext.Appointments
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.PatientId == patient.Id || a.Patient.PrimaryPatientId == patient.Id)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(ct);

        return appointments.Select(a => new MyAppointmentDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.Dentist.FullName,
            a.Dentist.ProfilePictureUrl,
            a.Dentist.Specialization,
            a.AppointmentDate,
            a.Status.ToString(),
            a.Symptoms,
            a.Service?.Name,
            a.Patient.FullName,
            a.Patient.Id == patient.Id ? "Tôi" : (a.Patient.Relationship ?? string.Empty),
            a.Patient.Id));
    }
}
