namespace DentalClinic.API.Domain.Schedules;

/// <summary>
/// Sinh danh sách khung giờ ứng viên (bước 30 phút) cho cả ngày làm việc, và xác định một khung giờ
/// có bị "chiếm" bởi một lịch hẹn đã đặt hay không — có tính đến thời lượng dịch vụ (không chỉ khớp
/// đúng giờ bắt đầu). Dùng chung cho <c>GetDentistSlotsHandler</c>, <c>GetFollowUpSlotsHandler</c> và
/// <c>CreateAppointmentHandler</c> để tránh lệch logic giữa các luồng.
/// </summary>
public static class SlotCalculator
{
    public const int SlotMinutes = 30;

    /// <summary>Khung giờ ứng viên bước 30 phút, trải từ ca sớm nhất đến ca muộn nhất trong <see cref="WorkShifts.All"/>.</summary>
    public static readonly IReadOnlyList<(int Hour, int Minute)> AllTimes = GenerateCandidates();

    private static List<(int Hour, int Minute)> GenerateCandidates()
    {
        var startMinutes = WorkShifts.All.Min(s => s.StartMinutes);
        var endMinutes = WorkShifts.All.Max(s => s.EndMinutes);

        var result = new List<(int, int)>();
        for (var m = startMinutes; m + SlotMinutes <= endMinutes; m += SlotMinutes)
            result.Add((m / 60, m % 60));
        return result;
    }

    /// <summary>Buổi (sáng/chiều/tối) của một thời điểm — dựa vào ca chứa nó, hoặc quy tắc giờ-trong-ngày cho khoảng trống giữa các ca (nghỉ trưa) / lịch cũ.</summary>
    public static string PeriodAt(int hour, int minute)
    {
        var m = hour * 60 + minute;
        var match = WorkShifts.All.FirstOrDefault(s => m >= s.StartMinutes && m < s.EndMinutes);
        if (match is not null) return match.Period;

        if (m < 12 * 60) return WorkShifts.PeriodMorning;
        if (m < 17 * 60 + 30) return WorkShifts.PeriodAfternoon;
        return WorkShifts.PeriodEvening;
    }

    /// <summary>Một khoảng thời gian (phút trong ngày) đã bị chiếm bởi một lịch hẹn.</summary>
    public record OccupiedRange(int StartMinutes, int EndMinutes);

    /// <summary>Khung giờ [slotStart, slotEnd) có chồng lấn với bất kỳ khoảng đã bị chiếm nào không.</summary>
    public static bool IsOccupied(int slotStartMinutes, int slotEndMinutes, IEnumerable<OccupiedRange> ranges) =>
        ranges.Any(r => slotStartMinutes < r.EndMinutes && slotEndMinutes > r.StartMinutes);

    /// <summary>
    /// Khoảng thời gian một lịch hẹn chiếm dụng, dựa theo thời lượng dịch vụ đã đặt.
    /// Mặc định 30 phút nếu lịch hẹn không gắn dịch vụ hoặc dịch vụ không có thời lượng hợp lệ.
    /// </summary>
    public static OccupiedRange BuildOccupiedRange(int startHour, int startMinute, int? serviceDurationMinutes)
    {
        var start = startHour * 60 + startMinute;
        var duration = serviceDurationMinutes is > 0 ? serviceDurationMinutes.Value : SlotMinutes;
        return new OccupiedRange(start, start + duration);
    }
}
