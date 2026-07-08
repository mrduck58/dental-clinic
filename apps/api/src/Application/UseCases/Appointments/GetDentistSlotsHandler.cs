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
        // Kiểm tra WorkSchedule cho ngày này
        var daySchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == date)
            .ToListAsync(ct);

        // Nếu có WorkSchedule cho ngày này và mark là nghỉ lễ
        var holidaySchedule = daySchedules.FirstOrDefault(ws => ws.IsHoliday);
        if (holidaySchedule != null)
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Nếu không có WorkSchedule nào cho ngày này và là Chủ Nhật (weekday = 7)
        if (daySchedules.Count == 0 && date.DayOfWeek == DayOfWeek.Sunday)
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Nếu không có WorkSchedule nào cho ngày này (trừ ngày nghỉ lễ đã check ở trên)
        // → Không cho phép đặt lịch
        if (daySchedules.Count == 0)
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Lấy WorkSchedule cho bác sĩ (type = "dentist")
        var dentistSchedules = daySchedules
            .Where(ws => ws.Type == "dentist")
            .ToList();

        // Chỉ lấy bác sĩ có trong WorkSchedule của ngày đó
        var dentistNames = dentistSchedules
            .Select(ws => ws.StaffName)
            .ToHashSet();

        var dentists = await dbContext.Dentists
            .Include(d => d.User)
            .Where(d => dentistNames.Contains(d.FullName))
            .ToListAsync(ct);
        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        return dentists.Select(d =>
        {
            var bookedTimes = dayAppointments
                .Where(a => a.DentistId == d.Id)
                .Select(a =>
                {
                    var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                    return (localTime.Hour, localTime.Minute);
                })
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
