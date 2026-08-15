using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dentists;

/// <summary>
/// GET api/dentists/{id}/working-dates — các ngày trong tháng bác sĩ còn slot trống.
/// Tách ra từ method thứ hai của <c>GetDentistSlotsHandler</c> theo chuẩn 1 handler = 1 use case.
/// </summary>
public record GetDentistWorkingDatesQuery(Guid DentistId, int Year, int Month) : IRequest<IEnumerable<string>>;

public class GetDentistWorkingDatesHandler(
    IDentistRepository dentistRepository,
    IUserRepository userRepository,
    IAppointmentRepository appointmentRepository,
    IWorkScheduleRepository workScheduleRepository)
    : IRequestHandler<GetDentistWorkingDatesQuery, IEnumerable<string>>
{
    public async Task<IEnumerable<string>> Handle(GetDentistWorkingDatesQuery request, CancellationToken ct)
    {
        var dentistId = request.DentistId;
        var year = request.Year;
        var month = request.Month;

        var dentist = await dentistRepository.GetByIdOrUserIdAsync(dentistId, ct);

        string fullName;
        Guid realDentistId;
        string defaultShift;

        if (dentist != null)
        {
            fullName = dentist.FullName;
            realDentistId = dentist.Id;
            defaultShift = dentist.Shift;
        }
        else
        {
            var user = await userRepository.GetByIdAsync(dentistId, ct);
            if (user == null) return Enumerable.Empty<string>();
            fullName = user.FullName ?? string.Empty;
            realDentistId = user.Id;
            defaultShift = "FullTime";
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Lấy tất cả lịch hẹn chưa hủy trong tháng của bác sĩ này
        var dentistAppointments = await appointmentRepository.GetActiveByDentistIdAsync(realDentistId, ct);

        var appointmentsByDate = dentistAppointments
            .GroupBy(a => DateOnly.FromDateTime(a.AppointmentDate.UtcDateTime.AddHours(7)))
            .ToDictionary(g => g.Key, g => g.ToList());

        var schedules = await workScheduleRepository.GetByDateRangeAsync(startDate, endDate, ct);

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
            // 1. Ngày Lễ -> Không làm việc
            if (holidayDates.Contains(date))
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
