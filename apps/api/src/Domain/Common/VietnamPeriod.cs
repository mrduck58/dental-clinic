namespace DentalClinic.API.Domain.Common;

/// <summary>
/// Quy đổi một khoảng ngày (DateOnly, do client chọn theo giờ VN) thành mốc UTC để so sánh với
/// DateTimeOffset lưu trong DB — dùng chung cho mọi truy vấn "trong kỳ" (chi phí, vật tư, lương...) để
/// đảm bảo các con số tổng hợp và chi tiết drill-down luôn khớp nhau tuyệt đối, không lệch múi giờ.
/// </summary>
public static class VietnamPeriod
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>Start = 00:00 giờ VN của ngày sớm hơn (bao gồm); End = 00:00 giờ VN của ngày sau ngày muộn
    /// hơn (không bao gồm) — nên so sánh phải dùng <c>&gt;= Start &amp;&amp; &lt; End</c>.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) Bounds(DateOnly from, DateOnly to)
    {
        var (fromDate, toDate) = from > to ? (to, from) : (from, to);
        return (ToUtc(fromDate), ToUtc(toDate.AddDays(1)));
    }

    private static DateTimeOffset ToUtc(DateOnly date)
        => new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset).ToUniversalTime();
}
