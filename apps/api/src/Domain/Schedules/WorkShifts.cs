namespace DentalClinic.API.Domain.Schedules;

/// <summary>
/// Định nghĩa 6 ca làm việc NGANG HÀNG trong một ngày (2 sáng, 2 chiều, 2 tối).
/// Không còn khái niệm "ca sáng"/"ca chiều" là một đơn vị lớn không thể tách —
/// mỗi ca là một khung 2 tiếng độc lập.
///
/// <para>
/// Giá trị <see cref="ShiftDef.Code"/> được lưu trực tiếp vào cột
/// <c>WorkSchedule.Shift</c> (tối đa 20 ký tự). Các handler đặt lịch dùng
/// <see cref="IsWorkingAt"/> để quyết định một khung giờ có nằm trong ca được phân hay không.
/// </para>
///
/// <para>
/// Vẫn hỗ trợ dữ liệu CŨ lưu "morning"/"afternoon" để không phải migrate DB:
/// "morning" = trước 12:00, "afternoon" = từ 12:00 trở đi.
/// </para>
/// </summary>
public static class WorkShifts
{
    public sealed record ShiftDef(
        string Code,
        string Label,
        string Period,
        int StartMinutes,
        int EndMinutes);

    private static int Min(int hour, int minute) => hour * 60 + minute;

    public const string PeriodMorning   = "Buổi sáng";
    public const string PeriodAfternoon = "Buổi chiều";
    public const string PeriodEvening   = "Buổi tối";

    /// <summary>6 ca ngang hàng, theo thứ tự thời gian trong ngày.</summary>
    public static readonly IReadOnlyList<ShiftDef> All =
    [
        new("08:00-10:00", "08:00 – 10:00", PeriodMorning,   Min(8, 0),   Min(10, 0)),
        new("10:00-12:00", "10:00 – 12:00", PeriodMorning,   Min(10, 0),  Min(12, 0)),
        new("13:30-15:30", "13:30 – 15:30", PeriodAfternoon, Min(13, 30), Min(15, 30)),
        new("15:30-17:30", "15:30 – 17:30", PeriodAfternoon, Min(15, 30), Min(17, 30)),
        new("17:30-19:30", "17:30 – 19:30", PeriodEvening,   Min(17, 30), Min(19, 30)),
        new("19:30-21:30", "19:30 – 21:30", PeriodEvening,   Min(19, 30), Min(21, 30)),
    ];

    private static readonly Dictionary<string, ShiftDef> ByCode =
        All.ToDictionary(s => s.Code, StringComparer.OrdinalIgnoreCase);

    /// <summary>Toàn bộ mã ca hợp lệ — 6 mã ca mới + "morning"/"afternoon" (dữ liệu cũ).</summary>
    public static readonly IReadOnlyList<string> AllValidCodes =
        [.. All.Select(s => s.Code), "morning", "afternoon"];

    /// <summary>
    /// Khoảng thời gian trước khi một ca bắt đầu mà bác sĩ của ca đó được coi là "sắp vào ca":
    /// gần giờ giao ca, hàng đợi cho lễ tân chọn giao bệnh nhân cho bác sĩ hiện tại hay người sắp tới.
    /// </summary>
    public const int ShiftHandoverWindowMinutes = 60;

    private const int NoonMinutes = 12 * 60;

    /// <summary>
    /// Một mã ca cụ thể có bao trùm thời điểm (phút trong ngày) không?
    /// Ca là nửa khoảng [start, end): mốc giờ kết thúc KHÔNG thuộc ca, vì một lịch hẹn
    /// bắt đầu tại đó sẽ tràn sang ca kế tiếp. Nhờ vậy mỗi ca 2 tiếng sinh đúng 4 khung
    /// giờ 30 phút (vd. "08:00-10:00" → 08:00, 08:30, 09:00, 09:30), và mốc giao giữa
    /// hai ca liền kề (10:00, 15:30, 17:30, 19:30) chỉ thuộc về ca đứng sau.
    /// </summary>
    private static bool ShiftCovers(string shift, int minutesOfDay)
    {
        if (string.IsNullOrWhiteSpace(shift))
            return (minutesOfDay >= 8 * 60 && minutesOfDay < NoonMinutes) || (minutesOfDay >= 13 * 60 + 30 && minutesOfDay < 17 * 60 + 30);

        var cleaned = shift.Trim().Replace(" ", "").Replace("–", "-");
        if (ByCode.TryGetValue(cleaned, out var def))
            return minutesOfDay >= def.StartMinutes && minutesOfDay < def.EndMinutes;

        var s = shift.Trim().ToLower();
        if (s == "morning" || s == "sáng" || s == "ca sáng")
            return minutesOfDay >= 8 * 60 && minutesOfDay < NoonMinutes;
        if (s == "afternoon" || s == "chiều" || s == "ca chiều")
            return minutesOfDay >= 13 * 60 + 30 && minutesOfDay < 17 * 60 + 30;
        if (s == "evening" || s == "tối" || s == "ca tối")
            return minutesOfDay >= 17 * 60 + 30 && minutesOfDay < 21 * 60 + 30;
        if (s == "fulltime" || s == "full time" || s == "cả ngày" || s == "tất cả" || s == "toàn thời gian")
            return (minutesOfDay >= 8 * 60 && minutesOfDay < NoonMinutes) || (minutesOfDay >= 13 * 60 + 30 && minutesOfDay < 17 * 60 + 30);

        return (minutesOfDay >= 8 * 60 && minutesOfDay < NoonMinutes) || (minutesOfDay >= 13 * 60 + 30 && minutesOfDay < 17 * 60 + 30);
    }

    /// <summary>
    /// Trong tập ca được phân cho một nhân sự trong ngày, có ca nào bao trùm
    /// khung giờ <paramref name="hour"/>:<paramref name="minute"/> không?
    /// Đây là primitive để sinh khung giờ đặt lịch.
    /// </summary>
    public static bool IsWorkingAt(IEnumerable<string> assignedShifts, int hour, int minute)
    {
        var mod = hour * 60 + minute;
        foreach (var s in assignedShifts)
            if (ShiftCovers(s, mod))
                return true;
        return false;
    }

    /// <summary>Nhóm buổi (sáng/chiều/tối) của một mã ca — dùng để hiển thị.</summary>
    public static string PeriodOf(string shift)
    {
        if (ByCode.TryGetValue(shift, out var def)) return def.Period;
        if (shift.Equals("afternoon", StringComparison.OrdinalIgnoreCase)) return PeriodAfternoon;
        return PeriodMorning;
    }

    /// <summary>Nhãn hiển thị "HH:mm – HH:mm" của một mã ca (null nếu là mã cũ/không xác định).</summary>
    public static string? LabelOf(string shift)
        => ByCode.TryGetValue(shift, out var def) ? def.Label : null;

    /// <summary>Thứ tự sắp xếp trong ngày của một mã ca (mã cũ/không xác định xếp cuối).</summary>
    public static int SortKey(string shift)
        => ByCode.TryGetValue(shift, out var def) ? def.StartMinutes : int.MaxValue;

    /// <summary>
    /// Ca có bắt đầu trong khoảng (<paramref name="nowMinutesOfDay"/>, +<paramref name="windowMinutes"/>]
    /// không? Dùng để nhận diện bác sĩ "sắp vào ca" gần thời điểm giao ca. Mốc bắt đầu đúng hiện tại
    /// đã thuộc "đang trong ca" nên KHÔNG tính là sắp tới. Mã cũ/không xác định (không biết giờ bắt
    /// đầu) → false.
    /// </summary>
    public static bool StartsWithinMinutes(string shift, int nowMinutesOfDay, int windowMinutes)
    {
        if (!ByCode.TryGetValue(shift, out var def)) return false;
        var delta = def.StartMinutes - nowMinutesOfDay;
        return delta > 0 && delta <= windowMinutes;
    }
}
