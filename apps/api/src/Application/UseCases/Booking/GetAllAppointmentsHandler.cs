using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Booking;

public record StaffAppointmentDto(
    Guid AppointmentId,
    string AppointmentCode,
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

public class GetAllAppointmentsHandler(AppDbContext dbContext)
    : IRequestHandler<GetAllAppointmentsQuery, IEnumerable<StaffAppointmentDto>>
{
    public async Task<IEnumerable<StaffAppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken ct)
    {
        var date = request.Date;
        var status = request.Status;

        var query = dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist).ThenInclude(d => d.User)
            .Include(a => a.Service)
            .AsQueryable();

        if (date.HasValue)
        {
            var start = new DateTimeOffset(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddDays(1);
            query = query.Where(a => a.AppointmentDate >= start && a.AppointmentDate < end);
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(a => a.Status == statusEnum);
        }

        var appointments = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return appointments.Select(a => new StaffAppointmentDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString()[..6].ToUpper()}",
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
