using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Booking;

public record StaffScheduleSlot(string Time, bool IsBooked, string? PatientName, bool IsPast);

public record StaffScheduleDentistDto(
    Guid DentistId,
    string Name,
    string Room,
    List<StaffScheduleSlot> Slots);

public record StaffScheduleResponse(
    DateOnly Date,
    List<StaffScheduleDentistDto> Dentists);

public record GetStaffScheduleQuery(DateOnly? Date) : IRequest<StaffScheduleResponse>;

public class GetStaffScheduleHandler(AppDbContext dbContext)
    : IRequestHandler<GetStaffScheduleQuery, StaffScheduleResponse>
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    // Lưới 30 phút cho cả ngày; mỗi khung chỉ hiện nếu nằm trong một ca bác sĩ được phân.
    private static readonly (int Hour, int Minute)[] AllTimes =
    [
        (8,0),(8,30),(9,0),(9,30),(10,0),(10,30),(11,0),(11,30),                 // sáng
        (13,30),(14,0),(14,30),(15,0),(15,30),(16,0),(16,30),(17,0),             // chiều
        (17,30),(18,0),(18,30),(19,0),(19,30),(20,0),(20,30),(21,0),             // tối
    ];

    public async Task<StaffScheduleResponse> Handle(GetStaffScheduleQuery request, CancellationToken ct)
    {
        var queryDate = request.Date;

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var date = queryDate ?? DateOnly.FromDateTime(vietnamNow);

        var vnStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset);
        var utcStart = vnStart.ToUniversalTime();
        var utcEnd   = utcStart.AddDays(1);

        // 1. Lịch làm việc hôm nay (bác sĩ, không phải ngày nghỉ)
        //    Chỉ chấp nhận mã ca hợp lệ (6 ca 2 tiếng hiện tại + "morning"/"afternoon" cũ) —
        //    dữ liệu rác với giá trị Shift khác không được coi là bác sĩ có ca làm việc thật.
        var validShiftCodes = WorkShifts.AllValidCodes;
        var todaySchedules = await dbContext.WorkSchedules
            .Where(s => s.Type == "dentist" && !s.IsHoliday && s.Date == date &&
                        validShiftCodes.Contains(s.Shift))
            .ToListAsync(ct);

        // Tên bác sĩ làm việc hôm nay → set ca làm việc (mã ca 2 tiếng hoặc "morning"/"afternoon" cũ)
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
                // EmploymentStatus là cột NOT NULL, dữ liệu cũ/chưa set lưu chuỗi rỗng chứ không
                // phải null — "?? "Active"" sẽ không bao giờ kích hoạt, nên phải coi rỗng là Active.
                (string.IsNullOrWhiteSpace(u.Dentist?.EmploymentStatus) ||
                 string.Equals(u.Dentist!.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase)) &&
                workingToday.ContainsKey(!string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : u.Email))
            .OrderBy(u => u.FullName)
            .ToList();

        // 3. Tự động tạo Dentist entry cho bác sĩ chưa có (để FK appointment hợp lệ)
        var createdDentists = new Dictionary<Guid, Dentist>();
        foreach (var user in dentistUsers.Where(u => u.Dentist == null))
        {
            var d = Dentist.Create(user.Id,
                        $"DT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                        "Nha khoa tổng quát",
                        "N/A",
                        experienceYears: 0);
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
            var name = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email;
            var shifts = workingToday.GetValueOrDefault(name, []);
            var dentistAppts = appointments.Where(a => a.DentistId == dentist.Id).ToList();
            var room = todaySchedules.FirstOrDefault(s => s.StaffName == name)?.Room ?? "—";

            // Chỉ hiện khung giờ nằm trong các ca bác sĩ được phân hôm nay.
            var slots = AllTimes
                .Where(t => WorkShifts.IsWorkingAt(shifts, t.Hour, t.Minute))
                .Select(t => BuildSlot(t.Hour, t.Minute, dentistAppts, date))
                .ToList();

            return new StaffScheduleDentistDto(dentist.Id, name, room, slots);
        })
        // Cột lưới xếp theo số phòng (Phòng 1 → 2 → 3...) để khớp thứ tự phòng ngoài đời.
        // Phòng không có số ("Phòng Test", "—") dồn xuống cuối, rồi sắp theo tên bác sĩ.
        .OrderBy(d => RoomSortKey(d.Room))
        .ThenBy(d => d.Name, StringComparer.CurrentCulture)
        .ToList();

        return new StaffScheduleResponse(date, result);
    }

    /// <summary>Số phòng lấy từ cụm chữ số đầu tiên trong tên phòng; không có thì xếp cuối.</summary>
    private static int RoomSortKey(string room)
    {
        var digits = string.Empty;
        foreach (var c in room)
        {
            if (char.IsDigit(c)) digits += c;
            else if (digits.Length > 0) break;
        }
        return digits.Length > 0 && int.TryParse(digits, out var n) ? n : int.MaxValue;
    }

    private static StaffScheduleSlot BuildSlot(int hour, int minute, List<Appointment> appts, DateOnly date)
    {
        var vnSlot = new DateTimeOffset(
            date.Year, date.Month, date.Day,
            hour, minute, 0,
            VietnamTz.BaseUtcOffset);
        var utcSlot = vnSlot.ToUniversalTime();

        var time   = $"{hour:D2}:{minute:D2}";
        var booked = appts.FirstOrDefault(a => a.AppointmentDate == utcSlot);
        var isPast = utcSlot < DateTimeOffset.UtcNow;
        return new StaffScheduleSlot(time, booked != null, booked?.Patient.FullName, isPast);
    }
}
