using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using DentalClinic.API.Infrastructure.Persistence;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record TimeSlotDto(string Range, bool IsBooked);

public record DentistWithSlotsDto(
    Guid DentistId,
    string FullName,
    string Specialization,
    string? AvatarUrl,
    string Shift,
    int ExperienceYears,
    List<TimeSlotDto> Slots);

public class GetDentistSlotsHandler(AppDbContext dbContext, IAppointmentRepository appointmentRepository)
{
    private static readonly (int Hour, int Minute)[] MorningTimes = [(7, 30), (8, 30), (9, 30), (10, 30)];
    private static readonly (int Hour, int Minute)[] AfternoonTimes = [(13, 30), (14, 30), (15, 30), (16, 30)];

    public async Task<IEnumerable<DentistWithSlotsDto>> HandleAsync(DateOnly date, CancellationToken ct = default)
    {
        var dentists = await dbContext.Dentists
            .Include(d => d.User)
            .ToListAsync(ct);

        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        return dentists.Select(d =>
        {
            var bookedTimes = dayAppointments
                .Where(a => a.DentistId == d.Id)
                .Select(a => (a.AppointmentDate.Hour, a.AppointmentDate.Minute))
                .ToHashSet();

            var times = d.Shift == "afternoon" ? AfternoonTimes : MorningTimes;
            var slots = times.Select(t =>
            {
                var range = $"{t.Hour:D2}:{t.Minute:D2} - {t.Hour + 1:D2}:{t.Minute:D2}";
                var isBooked = bookedTimes.Contains((t.Hour, t.Minute));
                return new TimeSlotDto(range, isBooked);
            }).ToList();

            return new DentistWithSlotsDto(
                d.Id,
                d.FullName,
                d.Specialization,
                d.User.ProfilePictureUrl,
                d.Shift,
                d.ExperienceYears,
                slots);
        });
    }
}
