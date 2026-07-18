using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using DentalClinic.API.Infrastructure.Persistence;

namespace DentalClinic.API.Application.UseCases.Appointments;

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

public class GetMyAppointmentsHandler(
    IPatientRepository patientRepository,
    AppDbContext dbContext)
{
    public async Task<IEnumerable<MyAppointmentDto>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var patient = await patientRepository.GetByUserIdAsync(userId, ct);
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
