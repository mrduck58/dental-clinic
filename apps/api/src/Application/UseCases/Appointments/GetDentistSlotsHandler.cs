using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using Microsoft.EntityFrameworkCore;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record TimeSlotDto(string Range, bool IsBooked, string Period);

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
    public async Task<IEnumerable<DentistWithSlotsDto>> HandleAsync(DateOnly date, CancellationToken ct = default)
    {
        // Chủ Nhật mặc định phòng khám nghỉ
        if (date.DayOfWeek == DayOfWeek.Sunday)
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Kiểm tra WorkSchedule cho ngày này
        var daySchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == date)
            .ToListAsync(ct);

        // Nếu có WorkSchedule đánh dấu là ngày nghỉ lễ
        if (daySchedules.Any(ws => ws.IsHoliday))
        {
            return Enumerable.Empty<DentistWithSlotsDto>();
        }

        // Lấy WorkSchedule liên quan đến bác sĩ
        var dentistSchedules = daySchedules
            .Where(ws => ws.Type == "dentist" || ws.Role == "dentist" || string.Equals(ws.Type, "Khám", StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<Dentist> dentists;
        var shiftsByName = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        if (dentistSchedules.Count > 0)
        {
            shiftsByName = dentistSchedules
                .GroupBy(ws => ws.StaffName)
                .ToDictionary(g => g.Key, g => g.Select(ws => ws.Shift).ToHashSet(), StringComparer.OrdinalIgnoreCase);

            var dentistNames = dentistSchedules
                .Select(ws => ws.StaffName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            dentists = await dbContext.Dentists
                .Include(d => d.User)
                .Where(d => d.User.IsActive && dentistNames.Contains(d.User.FullName ?? string.Empty))
                .ToListAsync(ct);
        }
        else
        {
            // Chưa có WorkSchedule cụ thể cho bác sĩ ngày này -> Mặc định lấy tất cả bác sĩ đang hoạt động
            dentists = await dbContext.Dentists
                .Include(d => d.User)
                .Where(d => d.User.IsActive)
                .ToListAsync(ct);
        }

        var dayAppointments = await appointmentRepository.GetByDateAsync(date, ct);

        return dentists.Select(d =>
        {
            var occupiedRanges = dayAppointments
                .Where(a => a.DentistId == d.Id)
                .Select(a =>
                {
                    var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                    return SlotCalculator.BuildOccupiedRange(localTime.Hour, localTime.Minute, a.Service?.DurationMinutes);
                })
                .ToList();

            // Khung giờ theo các ca THỰC TẾ được phân trong ngày; dự phòng dùng ca tĩnh của bác sĩ
            var assignedShifts = shiftsByName.TryGetValue(d.FullName, out var s) && s.Count > 0
                ? (IEnumerable<string>)s
                : [d.Shift];

            var slots = SlotCalculator.AllTimes
                .Where(t => WorkShifts.IsWorkingAt(assignedShifts, t.Hour, t.Minute))
                .Select(t =>
                {
                    var slotStart = t.Hour * 60 + t.Minute;
                    var slotEnd = slotStart + SlotCalculator.SlotMinutes;
                    var range = $"{t.Hour:D2}:{t.Minute:D2} - {slotEnd / 60:D2}:{slotEnd % 60:D2}";
                    var isBooked = SlotCalculator.IsOccupied(slotStart, slotEnd, occupiedRanges);
                    var period = SlotCalculator.PeriodAt(t.Hour, t.Minute);
                    return new TimeSlotDto(range, isBooked, period);
                }).ToList();

            return new DentistWithSlotsDto(
                d.Id,
                d.FullName,
                d.Specialization,
                d.ProfilePictureUrl,
                d.Shift,
                d.ExperienceYears ?? 0,
                slots);
        });
    }

    public async Task<IEnumerable<string>> GetWorkingDatesForDentistAsync(
        Guid dentistId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        var dentist = await dbContext.Dentists
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == dentistId || d.UserId == dentistId, ct);

        string fullName;
        Guid realDentistId;
        string defaultShift;

        if (dentist != null)
        {
            fullName = dentist.FullName ?? dentist.User?.FullName ?? string.Empty;
            realDentistId = dentist.Id;
            defaultShift = dentist.Shift;
        }
        else
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == dentistId, ct);
            if (user == null) return Enumerable.Empty<string>();
            fullName = user.FullName ?? string.Empty;
            realDentistId = user.Id;
            defaultShift = "FullTime";
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Lấy tất cả lịch hẹn chưa hủy trong tháng của bác sĩ này
        var dentistAppointments = await dbContext.Appointments
            .Include(a => a.Service)
            .Where(a => a.DentistId == realDentistId && a.Status != AppointmentStatus.Cancelled)
            .ToListAsync(ct);

        var appointmentsByDate = dentistAppointments
            .GroupBy(a => DateOnly.FromDateTime(a.AppointmentDate.UtcDateTime.AddHours(7)))
            .ToDictionary(g => g.Key, g => g.ToList());

        var schedules = await dbContext.WorkSchedules
            .AsNoTracking()
            .Where(ws => ws.Date >= startDate && ws.Date <= endDate)
            .ToListAsync(ct);

        var holidayDates = schedules
            .Where(ws => ws.IsHoliday)
            .Select(ws => ws.Date)
            .ToHashSet();

        var shiftsByDate = schedules
            .Where(ws => (ws.Type == "dentist" || ws.Role == "dentist" || string.Equals(ws.Type, "Khám", StringComparison.OrdinalIgnoreCase))
                         && IsStaffNameMatch(ws.StaffName, fullName)
                         && !ws.IsHoliday)
            .GroupBy(ws => ws.Date)
            .ToDictionary(g => g.Key, g => g.Select(ws => ws.Shift).ToList());

        var availableDates = new List<string>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // 1. Ngày Chủ Nhật hoặc Ngày Lễ -> Không làm việc
            if (date.DayOfWeek == DayOfWeek.Sunday || holidayDates.Contains(date))
                continue;

            // 2. Xác định ca trực thực tế trong ngày
            IEnumerable<string> assignedShifts;
            if (shiftsByDate.TryGetValue(date, out var customShifts) && customShifts.Count > 0)
            {
                if (customShifts.All(s => string.Equals(s, "Off", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "Nghỉ", StringComparison.OrdinalIgnoreCase)))
                    continue;
                assignedShifts = customShifts;
            }
            else if (schedules.Any(ws => ws.Date == date && (ws.Type == "dentist" || ws.Role == "dentist")))
            {
                // Có phân ca trong ngày cho bác sĩ khác nhưng không phân cho bác sĩ này -> Bác sĩ nghỉ
                continue;
            }
            else
            {
                // Chưa có ca lẻ phân trong hệ thống -> Dùng ca mặc định của bác sĩ
                assignedShifts = [defaultShift];
            }

            // 3. Tính danh sách khung giờ đã bị bận do lịch hẹn đã đặt
            var dayApps = appointmentsByDate.GetValueOrDefault(date) ?? [];
            var occupiedRanges = dayApps.Select(a =>
            {
                var localTime = a.AppointmentDate.UtcDateTime.AddHours(7);
                return SlotCalculator.BuildOccupiedRange(localTime.Hour, localTime.Minute, a.Service?.DurationMinutes);
            }).ToList();

            // 4. Kiểm tra có ít nhất 1 khung giờ chưa bị kín chỗ
            var hasAvailableSlot = SlotCalculator.AllTimes
                .Where(t => WorkShifts.IsWorkingAt(assignedShifts, t.Hour, t.Minute))
                .Any(t =>
                {
                    var slotStart = t.Hour * 60 + t.Minute;
                    var slotEnd = slotStart + SlotCalculator.SlotMinutes;
                    return !SlotCalculator.IsOccupied(slotStart, slotEnd, occupiedRanges);
                });

            if (hasAvailableSlot)
            {
                availableDates.Add(date.ToString("yyyy-MM-dd"));
            }
        }

        return availableDates;
    }

    private static bool IsStaffNameMatch(string staffName, string fullName)
    {
        if (string.IsNullOrWhiteSpace(staffName) || string.IsNullOrWhiteSpace(fullName)) return false;
        var cleanStaff = CleanName(staffName);
        var cleanFull = CleanName(fullName);
        return string.Equals(cleanStaff, cleanFull, StringComparison.OrdinalIgnoreCase)
               || cleanStaff.Contains(cleanFull, StringComparison.OrdinalIgnoreCase)
               || cleanFull.Contains(cleanStaff, StringComparison.OrdinalIgnoreCase);
    }

    private static string CleanName(string name)
    {
        return name.Replace("Bác sĩ", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("BS.", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("BS", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("Dr.", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("Dr", "", StringComparison.OrdinalIgnoreCase)
                   .Replace(".", "")
                   .Trim();
    }
}
