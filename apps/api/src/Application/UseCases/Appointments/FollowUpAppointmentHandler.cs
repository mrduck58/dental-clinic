using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record CreateFollowUpRequest(
    Guid OriginalAppointmentId,
    DateTimeOffset AppointmentDate,
    string? Symptoms,
    Guid? ServiceId,
    string? Notes);

public class FollowUpAppointmentHandler(AppDbContext dbContext)
{
    public async Task<FollowUpAppointmentDto> CreateAsync(CreateFollowUpRequest request, CancellationToken ct = default)
    {
        var originalAppointment = await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .FirstOrDefaultAsync(a => a.Id == request.OriginalAppointmentId, ct);

        if (originalAppointment == null)
            throw new KeyNotFoundException("Không tìm thấy lịch hẹn gốc.");

        var followUpAppointment = Appointment.CreateFollowUp(
            request.OriginalAppointmentId,
            originalAppointment.PatientId,
            originalAppointment.DentistId,
            request.AppointmentDate,
            request.Symptoms,
            request.ServiceId,
            request.Notes);

        dbContext.Appointments.Add(followUpAppointment);
        await dbContext.SaveChangesAsync(ct);

        // Reload with navigation properties
        var createdAppointment = await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .FirstAsync(a => a.Id == followUpAppointment.Id, ct);

        return ToDto(createdAppointment);
    }

    public async Task<List<FollowUpAppointmentDto>> GetByOriginalAppointmentAsync(Guid originalAppointmentId, CancellationToken ct = default)
    {
        var appointments = await dbContext.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .Where(a => a.FollowUpFromAppointmentId == originalAppointmentId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(ct);

        return appointments.Select(ToDto).ToList();
    }

    public async Task DeleteAsync(Guid followUpId, CancellationToken ct = default)
    {
        var appointment = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == followUpId, ct);

        if (appointment == null)
            throw new KeyNotFoundException("Không tìm thấy lịch tái khám.");

        dbContext.Appointments.Remove(appointment);
        await dbContext.SaveChangesAsync(ct);
    }

    private static FollowUpAppointmentDto ToDto(Appointment appointment)
    {
        return new FollowUpAppointmentDto
        {
            Id = appointment.Id,
            AppointmentCode = $"DK{appointment.AppointmentDate:yyyyMMdd}{appointment.Id.ToString("N")[..6].ToUpper()}",
            AppointmentDate = appointment.AppointmentDate,
            Status = appointment.Status.ToString(),
            Symptoms = appointment.Symptoms,
            Notes = appointment.Notes,
            ServiceName = appointment.Service?.Name,
            DentistName = appointment.Dentist.FullName,
            IsFollowUp = true,
            FollowUpFromAppointmentId = appointment.FollowUpFromAppointmentId
        };
    }
}

public class FollowUpAppointmentDto
{
    public Guid Id { get; set; }
    public string AppointmentCode { get; set; } = string.Empty;
    public DateTimeOffset AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Symptoms { get; set; }
    public string? Notes { get; set; }
    public string? ServiceName { get; set; }
    public string DentistName { get; set; } = string.Empty;
    public bool IsFollowUp { get; set; }
    public Guid? FollowUpFromAppointmentId { get; set; }
}
