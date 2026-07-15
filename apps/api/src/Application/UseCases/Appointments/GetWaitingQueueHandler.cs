using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Schedules;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Appointments;

public record QueuePatientDto(
    Guid AppointmentId,
    string AppointmentCode,
    string PatientName,
    string? PatientPhone,
    string? ServiceName,
    string DentistName,
    DateTimeOffset AppointmentDate,
    DateTimeOffset? CheckedInAt,
    int QueueNumber,
    string Status,
    string? Symptoms,
    int WaitMinutes);

public record QueueDentistDto(
    Guid DentistId,
    string DentistName,
    string DentistColor,
    List<string> Shifts,
    bool IsOnShiftNow,
    bool IsOnShiftSoon);

public record RoomQueueDto(
    string? RoomName,
    List<QueueDentistDto> Dentists,
    List<QueuePatientDto> Patients);

public record WaitingQueueResponse(
    DateOnly Date,
    int TotalWaiting,
    int TotalInProgress,
    int TotalCompleted,
    List<RoomQueueDto> Rooms);

public class GetWaitingQueueHandler(AppDbContext dbContext)
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>Khóa gộp cho bác sĩ/lịch hẹn không xác định được phòng.</summary>
    private const string NoRoomKey = "";

    public async Task<WaitingQueueResponse> HandleAsync(DateOnly date, CancellationToken ct = default)
    {
        // Convert Vietnam local date to UTC range for database query
        // Vietnam is UTC+7, so Vietnam 00:00 = UTC 17:00 previous day
        var vietnamDateStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTimeZone.BaseUtcOffset);
        var utcStart = vietnamDateStart.ToUniversalTime(); // Start of Vietnam date in UTC
        var utcEnd = utcStart.AddDays(1); // Start of next Vietnam date in UTC

        var nowUtc = DateTimeOffset.UtcNow;
        var nowVietnam = TimeZoneInfo.ConvertTime(nowUtc, VietnamTimeZone);
        var isToday = date == DateOnly.FromDateTime(nowVietnam.DateTime);
        var nowMinutes = nowVietnam.Hour * 60 + nowVietnam.Minute;

        var appointments = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= utcStart && a.AppointmentDate < utcEnd &&
                        (a.Status == AppointmentStatus.CheckedIn ||
                         a.Status == AppointmentStatus.InProgress ||
                         a.Status == AppointmentStatus.Completed))
            .ToListAsync(ct);

        // Ca làm việc của bác sĩ trong ngày. Chỉ chấp nhận mã ca hợp lệ — dữ liệu rác
        // với giá trị Shift khác không được coi là bác sĩ có ca làm việc thật.
        var validShiftCodes = WorkShifts.AllValidCodes;
        var daySchedules = await dbContext.WorkSchedules
            .Where(ws => ws.Date == date && ws.Type == "dentist" && !ws.IsHoliday &&
                         validShiftCodes.Contains(ws.Shift))
            .ToListAsync(ct);

        var schedulesByName = daySchedules
            .GroupBy(ws => ws.StaffName)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(ws => WorkShifts.SortKey(ws.Shift)).ToList());

        // Hàng đợi hiện MỌI phòng có bác sĩ đang hoạt động — kể cả phòng chưa có ai check-in,
        // để lễ tân thấy được phòng nào đang trống. "Đang hoạt động" = có ca làm việc trong
        // ngày và tài khoản còn Active.
        var activeDentists = await dbContext.Dentists
            .Include(d => d.User)
            .ToListAsync(ct);

        var dentistsToShow = activeDentists
            .Where(d => schedulesByName.ContainsKey(d.FullName) &&
                        string.Equals(d.User?.EmploymentStatus, User.DefaultEmploymentStatus,
                                      StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Dự phòng: bác sĩ có bệnh nhân đã check-in nhưng không có ca trong bảng lịch làm việc
        // (lịch bị xóa, xếp lịch thiếu…). Vẫn phải hiện, nếu không hàng đợi của họ biến mất.
        var shownIds = dentistsToShow.Select(d => d.Id).ToHashSet();
        dentistsToShow.AddRange(appointments
            .Where(a => !shownIds.Contains(a.DentistId))
            .GroupBy(a => a.DentistId)
            .Select(g => g.First().Dentist));

        // Mỗi phòng một hàng đợi. Một bác sĩ có thể trực ở hai phòng khác nhau trong cùng ngày,
        // nên bác sĩ được xếp vào từng phòng theo đúng ca của mình, chứ không phải chỉ một phòng.
        var dentistsByRoom = new Dictionary<string, List<QueueDentistDto>>();
        foreach (var dentist in dentistsToShow)
        {
            var schedules = schedulesByName.GetValueOrDefault(dentist.FullName, []);
            var color = GetDentistColor(dentist.FullName);

            if (schedules.Count == 0)
            {
                AddDentist(dentistsByRoom, NoRoomKey, new QueueDentistDto(dentist.Id, dentist.FullName, color, [], false, false));
                continue;
            }

            foreach (var byRoom in schedules.GroupBy(ws => RoomKey(ws.Room)))
            {
                var shifts = byRoom
                    .OrderBy(ws => WorkShifts.SortKey(ws.Shift))
                    .Select(ws => WorkShifts.LabelOf(ws.Shift) ?? WorkShifts.PeriodOf(ws.Shift))
                    .ToList();

                var onShiftNow = isToday && byRoom.Any(ws => CoversNow(ws, nowVietnam));
                // "Sắp vào ca": chỉ tính khi bác sĩ CHƯA trực ngay bây giờ nhưng có ca bắt đầu trong
                // cửa sổ giao ca — để lễ tân giao trước bệnh nhân cho người sắp tới.
                var onShiftSoon = isToday && !onShiftNow && byRoom.Any(ws =>
                    WorkShifts.StartsWithinMinutes(ws.Shift, nowMinutes, WorkShifts.ShiftHandoverWindowMinutes));

                AddDentist(dentistsByRoom, byRoom.Key, new QueueDentistDto(dentist.Id, dentist.FullName, color, shifts, onShiftNow, onShiftSoon));
            }
        }

        var patientsByRoom = appointments
            .GroupBy(a => RoomForAppointment(a, schedulesByName, isToday, nowVietnam))
            .ToDictionary(g => g.Key, g => g.ToList());

        var rooms = dentistsByRoom.Keys
            .Union(patientsByRoom.Keys)
            .Select(room => new RoomQueueDto(
                room == NoRoomKey ? null : room,
                dentistsByRoom.GetValueOrDefault(room, [])
                    .OrderBy(d => d.Shifts.Count > 0 ? d.Shifts[0] : string.Empty, StringComparer.Ordinal)
                    .ThenBy(d => d.DentistName, StringComparer.CurrentCulture)
                    .ToList(),
                BuildPatients(patientsByRoom.GetValueOrDefault(room, []), nowUtc)))
            .OrderBy(r => RoomSortKey(r.RoomName))
            .ThenBy(r => r.RoomName, StringComparer.CurrentCulture)
            .ToList();

        return new WaitingQueueResponse(
            date,
            appointments.Count(a => a.Status == AppointmentStatus.CheckedIn),
            appointments.Count(a => a.Status == AppointmentStatus.InProgress),
            appointments.Count(a => a.Status == AppointmentStatus.Completed),
            rooms);
    }

    private static void AddDentist(Dictionary<string, List<QueueDentistDto>> byRoom, string room, QueueDentistDto dentist)
    {
        if (!byRoom.TryGetValue(room, out var list))
            byRoom[room] = list = [];
        list.Add(dentist);
    }

    private static string RoomKey(string? room) => string.IsNullOrWhiteSpace(room) ? NoRoomKey : room;

    private static bool CoversNow(WorkSchedule schedule, DateTimeOffset nowVietnam)
        => WorkShifts.IsWorkingAt([schedule.Shift], nowVietnam.Hour, nowVietnam.Minute);

    /// <summary>
    /// Phòng của một lịch hẹn trong hàng đợi.
    /// <para>
    /// Hàng đợi hôm nay là ảnh chụp của HIỆN TẠI: bệnh nhân chờ ở phòng mà bác sĩ của họ
    /// đang ngồi lúc này, chứ không phải phòng ứng với giờ hẹn (bác sĩ có thể đã đổi phòng
    /// giữa hai ca). Nhờ vậy khi lễ tân kéo bệnh nhân sang phòng khác — thao tác đổi bác sĩ
    /// phụ trách sang người đang trực phòng đó — bệnh nhân ở lại đúng phòng vừa thả.
    /// </para>
    /// <para>Xem ngày trong quá khứ thì không có "hiện tại", lùi về ca bao trùm giờ hẹn.</para>
    /// <para>Không tìm được ca nào bao trùm thì lùi tiếp về phòng của ca đầu ngày.</para>
    /// </summary>
    private static string RoomForAppointment(
        Appointment appointment,
        Dictionary<string, List<WorkSchedule>> schedulesByName,
        bool isToday,
        DateTimeOffset nowVietnam)
    {
        var schedules = schedulesByName.GetValueOrDefault(appointment.Dentist.FullName, []);
        if (schedules.Count == 0) return NoRoomKey;

        var current = isToday
            ? schedules.FirstOrDefault(ws => CoversNow(ws, nowVietnam))
            : null;

        if (current == null)
        {
            var vn = TimeZoneInfo.ConvertTime(appointment.AppointmentDate, VietnamTimeZone);
            current = schedules.FirstOrDefault(ws => WorkShifts.IsWorkingAt([ws.Shift], vn.Hour, vn.Minute));
        }

        return RoomKey((current ?? schedules[0]).Room);
    }

    /// <summary>
    /// Hàng đợi của một phòng: người ĐANG KHÁM đứng đầu, rồi tới những người đang chờ.
    /// Thứ tự chờ là thứ tự CHECK-IN, không phải giờ đặt lịch: ai đến trước khám trước.
    /// Lịch hẹn cũ (trước khi có cột CheckedInAt) lùi về giờ hẹn để vẫn xếp được thứ tự.
    /// Người đã khám xong rời khỏi hàng đợi.
    /// <para>
    /// Số thứ tự là số CỐ ĐỊNH theo thứ tự check-in trong ngày của phòng — gán một lần và KHÔNG
    /// dồn lên khi người phía trước được gọi vào khám hay khám xong. Vì vậy số được đánh trên MỌI
    /// lịch hẹn đã check-in (kể cả đang khám và đã xong), giống hệ thống bốc số: số ai người nấy giữ,
    /// danh sách chờ còn lại có thể khuyết số ở đầu.
    /// </para>
    /// </summary>
    /// <summary>
    /// VỊ TRÍ HIỂN THỊ: ưu tiên <see cref="Appointment.QueueOrder"/> do lễ tân đặt (đẩy lên/xuống,
    /// chuyển phòng); chưa đặt thì lùi về giờ check-in. Cùng đơn vị tick UTC nên so sánh nhất quán.
    /// </summary>
    private static long EffectiveOrder(Appointment a)
        => a.QueueOrder ?? (a.CheckedInAt ?? a.AppointmentDate).UtcTicks;

    /// <summary>
    /// MỐC ĐÁNH SỐ: <see cref="Appointment.QueueEntryOrder"/> — thời điểm vào hàng đợi của phòng.
    /// Không đổi khi đẩy lên/xuống nên số của mỗi người giữ nguyên; đặt lại khi chuyển phòng nên bệnh
    /// nhân nhận số mới ở cuối phòng mới. Chưa đặt thì lùi về giờ check-in.
    /// </summary>
    private static long EntryOrder(Appointment a)
        => a.QueueEntryOrder ?? (a.CheckedInAt ?? a.AppointmentDate).UtcTicks;

    private static List<QueuePatientDto> BuildPatients(List<Appointment> appointments, DateTimeOffset nowUtc)
    {
        // Số thứ tự đánh theo MỐC VÀO HÀNG ĐỢI PHÒNG (tăng dần theo thứ tự vào phòng): giữ nguyên khi
        // lễ tân đẩy lên/xuống, và đánh mới ở cuối khi chuyển phòng. Chỉ VỊ TRÍ hiển thị đổi theo
        // QueueOrder (bên dưới). Id làm mốc phá hòa cho ổn định qua mỗi lần tải lại.
        var stableNumbers = appointments
            .Where(a => a.Status is AppointmentStatus.CheckedIn
                                 or AppointmentStatus.InProgress
                                 or AppointmentStatus.Completed)
            .OrderBy(EntryOrder)
            .ThenBy(a => a.Id)
            .Select((a, i) => (a.Id, Number: i + 1))
            .ToDictionary(x => x.Id, x => x.Number);

        var ordered = appointments
            .Where(a => a.Status is AppointmentStatus.InProgress or AppointmentStatus.CheckedIn)
            .OrderBy(a => a.Status == AppointmentStatus.InProgress ? 0 : 1)
            .ThenBy(EffectiveOrder)
            .ThenBy(a => a.Id)
            .ToList();

        return ordered.Select(a => new QueuePatientDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.Patient.FullName,
            a.Patient.User?.PhoneNumber,
            a.Service?.Name,
            a.Dentist.FullName,
            a.AppointmentDate,
            a.CheckedInAt,
            stableNumbers.GetValueOrDefault(a.Id),
            a.Status.ToString(),
            a.Symptoms,
            // Thời gian chờ tính từ lúc CHECK-IN, không phải từ giờ hẹn.
            a.Status == AppointmentStatus.CheckedIn
                ? Math.Max(0, (int)(nowUtc - (a.CheckedInAt ?? a.AppointmentDate)).TotalMinutes)
                : 0
        )).ToList();
    }

    /// <summary>Số phòng lấy từ cụm chữ số đầu tiên trong tên phòng; không có thì xếp cuối.</summary>
    private static int RoomSortKey(string? room)
    {
        if (room == null) return int.MaxValue;

        var digits = string.Empty;
        foreach (var c in room)
        {
            if (char.IsDigit(c)) digits += c;
            else if (digits.Length > 0) break;
        }
        return digits.Length > 0 && int.TryParse(digits, out var n) ? n : int.MaxValue;
    }

    private static string GetDentistColor(string dentistName)
    {
        return dentistName.ToLowerInvariant() switch
        {
            var n when n.Contains("thảo") => "sky",
            var n when n.Contains("minh") => "violet",
            var n when n.Contains("linh") => "rose",
            var n when n.Contains("hùng") => "amber",
            _ => "slate"
        };
    }
}
