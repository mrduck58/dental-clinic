using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Schedules;
using MediatR;


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

public class GetStaffScheduleHandler(
    IWorkScheduleRepository workScheduleRepository,
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IDentistRepository dentistRepository,
    IAppointmentRepository appointmentRepository)
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
        var todaySchedules = await workScheduleRepository.GetDentistSchedulesForDateAsync(date, ct: ct);

        if (todaySchedules.Count == 0)
            return new StaffScheduleResponse(date, []);

        // Dò dòng lịch về đúng bác sĩ theo KHÓA NGOẠI. Đối chiếu theo tên chỉ còn là lưới an toàn
        // cho các dòng lưu trước khi có khóa: tên ở hai bảng viết khác nhau ("Đỗ Văn Phong" ở lịch
        // làm việc, "BS.Đỗ Văn Phong" ở hồ sơ), nên so chuỗi chính xác như trước làm bác sĩ biến
        // mất khỏi lưới đặt lịch mà không báo lỗi gì.
        var byEmployeeId = todaySchedules
            .Where(s => s.EmployeeId.HasValue)
            .ToLookup(s => s.EmployeeId!.Value);

        var byNameKey = todaySchedules
            .Where(s => !s.EmployeeId.HasValue)
            .ToLookup(s => StaffNameMatcher.Key(s.StaffName));

        List<WorkSchedule> RowsFor(Domain.Entities.User user)
        {
            if (user.Employee is { } employee)
            {
                var byId = byEmployeeId[employee.Id].ToList();
                if (byId.Count > 0) return byId;
            }

            var key = StaffNameMatcher.Key(!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email);
            return key.Length == 0 ? [] : byNameKey[key].ToList();
        }

        // 2. Lấy bác sĩ Active từ bảng Users, kèm Employee/DentistProfile
        var allUsers = await userRepository.GetAllAsync(ct);

        var dentistUsers = allUsers
            .Where(u =>
                u.Role == UserRole.Dentist &&
                // EmploymentStatus là cột NOT NULL, dữ liệu cũ/chưa set lưu chuỗi rỗng chứ không
                // phải null — "?? "Active"" sẽ không bao giờ kích hoạt, nên phải coi rỗng là Active.
                (string.IsNullOrWhiteSpace(u.Employee?.EmploymentStatus) ||
                 string.Equals(u.Employee!.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase)) &&
                RowsFor(u).Count > 0)
            .OrderBy(u => u.FullName)
            .ToList();

        if (dentistUsers.Count == 0)
            return new StaffScheduleResponse(date, []);

        // 3. Tự động tạo Employee + DentistProfile cho bác sĩ chưa có (để FK appointment hợp lệ)
        var createdDentists = new Dictionary<Guid, DentistProfile>();
        foreach (var user in dentistUsers.Where(u => u.Employee?.DentistProfile == null))
        {
            var employee = user.Employee;
            if (employee == null)
            {
                employee = Employee.Create(user.Id, $"NV-{Guid.NewGuid().ToString("N")[..8].ToUpper()}");
                await employeeRepository.AddAsync(employee, ct);
                user.AttachEmployee(employee);
                await userRepository.UpdateAsync(user, ct);
            }
            var d = DentistProfile.Create(employee.Id,
                        "Nha khoa tổng quát",
                        "N/A",
                        experienceYears: 0);
            await dentistRepository.AddAsync(d, ct);
            createdDentists[user.Id] = d;
        }

        // 4. Lịch hẹn hôm nay
        var appointments = await appointmentRepository.GetActiveInRangeAsync(utcStart, utcEnd, ct);

        // 5. Build kết quả — chỉ hiện slot của ca bác sĩ đang làm hôm nay
        var result = dentistUsers.Select(user =>
        {
            var dentist = user.Employee?.DentistProfile ?? createdDentists[user.Id];
            var name = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email;
            // Cùng một nguồn dòng lịch cho cả ca lẫn phòng — trước đây phòng được dò lại bằng phép
            // so tên chính xác lần nữa, nên bác sĩ vào được lưới vẫn có thể hiện phòng là "—".
            var rows = RowsFor(user);
            var shifts = rows.Select(s => s.Shift).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var dentistAppts = appointments.Where(a => a.DentistId == dentist.Id).ToList();
            var room = rows.Select(s => s.Room).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r)) ?? "—";

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
        var slotStartMinutes = hour * 60 + minute;

        var time = $"{hour:D2}:{minute:D2}";

        // 1. Tìm lịch hẹn bắt đầu đúng khung giờ này (ưu tiên lấy thông tin bệnh nhân chính)
        var exactMatch = appts.FirstOrDefault(a => a.AppointmentDate == utcSlot);

        // 2. Nếu không có lịch hẹn bắt đầu đúng khung giờ này, kiểm tra xem có lịch hẹn nào trước đó kéo dài trùm qua khung giờ này không
        var overlappingMatch = exactMatch ?? appts.FirstOrDefault(a =>
        {
            var aLocal = a.AppointmentDate.UtcDateTime.AddHours(7);
            var aStart = aLocal.Hour * 60 + aLocal.Minute;
            var duration = (a.Service != null && a.Service.DurationMinutes > 0) ? a.Service.DurationMinutes : SlotCalculator.SlotMinutes;
            var aEnd = aStart + duration;
            return slotStartMinutes >= aStart && slotStartMinutes < aEnd;
        });

        var isBooked = overlappingMatch != null;
        var isPast = utcSlot < DateTimeOffset.UtcNow;
        return new StaffScheduleSlot(time, isBooked, overlappingMatch?.Patient.FullName, isPast);
    }
}
