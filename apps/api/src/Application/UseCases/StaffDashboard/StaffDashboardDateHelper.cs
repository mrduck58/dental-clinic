namespace DentalClinic.API.Application.UseCases.StaffDashboard;

internal static class StaffDashboardDateHelper
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public static (DateTimeOffset Start, DateTimeOffset End) TodayVnRange()
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var today = DateOnly.FromDateTime(vietnamNow);

        // Nửa đêm (giờ VN) quy về UTC — Npgsql chỉ chấp nhận DateTimeOffset với Offset=0
        // khi ghi vào cột "timestamp with time zone".
        var start = new DateTimeOffset(today.Year, today.Month, today.Day, 0, 0, 0, VietnamTz.BaseUtcOffset)
            .ToUniversalTime();
        return (start, start.AddDays(1));
    }
}
