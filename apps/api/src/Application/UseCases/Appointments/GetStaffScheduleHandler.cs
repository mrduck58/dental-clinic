using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record StaffScheduleSlot(string Time, bool IsBooked, string? PatientName);

public record StaffScheduleDentistDto(
    Guid DentistId,
    string Name,
    string Room,
    List<StaffScheduleSlot> MorningSlots,
    List<StaffScheduleSlot> AfternoonSlots);

public record StaffScheduleResponse(
    DateOnly Date,
    List<StaffScheduleDentistDto> Dentists);

public class GetStaffScheduleHandler(AppDbContext dbContext)
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private static readonly string[] MorningTimes =
        ["08:00","08:30","09:00","09:30","10:00","10:30","11:00","11:30"];

    private static readonly string[] AfternoonTimes =
        ["13:00","13:30","14:00","14:30","15:00","15:30","16:00","16:30"];

    public async Task<StaffScheduleResponse> HandleAsync(DateOnly? queryDate, CancellationToken ct = default)
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var date = queryDate ?? DateOnly.FromDateTime(vietnamNow);

        var vnStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);
        var utcStart = vnStart.ToUniversalTime();
        var utcEnd   = utcStart.AddDays(1);

        // 1. Lịch làm việc hôm nay (bác sĩ, không phải ngày nghỉ)
        var todaySchedules = await dbContext.WorkSchedules
            .Where(s => s.Type == "dentist" && !s.IsHoliday && s.Date == date)
            .ToListAsync(ct);

        // Tên bác sĩ làm việc hôm nay → set ca làm việc ("morning"/"afternoon")
        var workingToday = todaySchedules
            .GroupBy(s => s.StaffName)
            .ToDictionary(g => g.Key, g => g.Select(s => s.Shift).ToHashSet(StringComparer.OrdinalIgnoreCase));

        if (workingToday.Count == 0)
            return new StaffScheduleResponse(date, []);

        // 2. Lấy bác sĩ Active từ bảng Users, kèm Dentist entry
        var allUsers = await dbContext.Users
            .Include(u => u.Dentist)
            .ToListAsync(ct);

        var dentistUsers = allUsers
            .Where(u =>
                (string.Equals(u.Role, "Dentist", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.Role, "Doctor",  StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(u.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase) &&
                workingToday.ContainsKey(u.FullName ?? u.Email))
            .OrderBy(u => u.FullName)
            .ToList();

        // 3. Tự động tạo Dentist entry cho bác sĩ chưa có (để FK appointment hợp lệ)
        var createdDentists = new Dictionary<Guid, Dentist>();
        foreach (var user in dentistUsers.Where(u => u.Dentist == null))
        {
            var d = Dentist.Create(user.Id, user.FullName ?? user.Email,
                        user.Specialty ?? "Nha khoa tổng quát",
                        user.YearsOfExperience ?? 0);
            dbContext.Dentists.Add(d);
            createdDentists[user.Id] = d;
        }
        if (createdDentists.Count > 0)
            await dbContext.SaveChangesAsync(ct);

        // 4. Lịch hẹn hôm nay
        var appointments = await dbContext.Appointments
            .Include(a => a.Patient)
            .Where(a => a.AppointmentDate >= utcStart &&
                        a.AppointmentDate < utcEnd &&
                        a.Status != AppointmentStatus.Cancelled)
            .ToListAsync(ct);

        // 5. Build kết quả — chỉ hiện slot của ca bác sĩ đang làm hôm nay
        var result = dentistUsers.Select(user =>
        {
            var dentist = user.Dentist ?? createdDentists[user.Id];
            var name = user.FullName ?? user.Email;
            var shifts = workingToday.GetValueOrDefault(name, []);
            var dentistAppts = appointments.Where(a => a.DentistId == dentist.Id).ToList();
            var room = todaySchedules.FirstOrDefault(s => s.StaffName == name)?.Room ?? "—";

            // Nếu không có thông tin ca cụ thể thì hiện cả hai ca
            var showMorning   = shifts.Count == 0 || shifts.Contains("morning");
            var showAfternoon = shifts.Count == 0 || shifts.Contains("afternoon");

            var morningSlots   = showMorning
                ? MorningTimes.Select(t   => BuildSlot(t, dentistAppts, date)).ToList()
                : [];
            var afternoonSlots = showAfternoon
                ? AfternoonTimes.Select(t => BuildSlot(t, dentistAppts, date)).ToList()
                : [];

            return new StaffScheduleDentistDto(dentist.Id, name, room, morningSlots, afternoonSlots);
        }).ToList();

        return new StaffScheduleResponse(date, result);
    }

    private static StaffScheduleSlot BuildSlot(string time, List<Appointment> appts, DateOnly date)
    {
        var parts  = time.Split(':');
        var vnSlot = new DateTimeOffset(
            date.Year, date.Month, date.Day,
            int.Parse(parts[0]), int.Parse(parts[1]), 0,
            VietnamTz.BaseUtcOffset);
        var utcSlot = vnSlot.ToUniversalTime();

        var booked = appts.FirstOrDefault(a => a.AppointmentDate == utcSlot);
        return new StaffScheduleSlot(time, booked != null, booked?.Patient.FullName);
    }
}
