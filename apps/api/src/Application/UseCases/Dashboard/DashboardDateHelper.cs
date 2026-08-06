using DentalClinic.API.Domain.Exceptions;

namespace DentalClinic.API.Application.UseCases.Dashboard;

/// <summary>Helper tính khoảng thời gian (giờ VN) dùng chung giữa các Dashboard Query handler.</summary>
public static class DashboardDateHelper
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public static string NormalizeRange(string? range)
    {
        var normalized = range?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "week" or "month" or "year" => normalized,
            null or "" => "week",
            _ => throw new ValidationException($"Khoảng thời gian '{range}' không hợp lệ. Chỉ chấp nhận: week, month, year.")
        };
    }

    public static DateOnly GetVietnamToday()
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        return DateOnly.FromDateTime(vietnamNow);
    }

    /// <summary>
    /// Nửa đêm (giờ VN) của một ngày, quy về UTC — Npgsql chỉ chấp nhận DateTimeOffset
    /// với Offset=0 khi ghi vào cột "timestamp with time zone".
    /// </summary>
    public static DateTimeOffset ToVn(DateOnly date) =>
        new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset).ToUniversalTime();

    public static (DateOnly Start, DateOnly End) GetCurrentPeriodDates(string range, DateOnly today) => range switch
    {
        "week" => WeekBounds(today),
        "month" => (new DateOnly(today.Year, today.Month, 1), new DateOnly(today.Year, today.Month, 1).AddMonths(1)),
        "year" => (new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 1, 1).AddYears(1)),
        _ => throw new ValidationException($"Khoảng thời gian '{range}' không hợp lệ. Chỉ chấp nhận: week, month, year.")
    };

    public static (DateOnly Start, DateOnly End) GetPreviousPeriodDates(string range, DateOnly currentStart) => range switch
    {
        "week" => (currentStart.AddDays(-7), currentStart),
        "month" => (currentStart.AddMonths(-1), currentStart),
        "year" => (currentStart.AddYears(-1), currentStart),
        _ => throw new ValidationException($"Khoảng thời gian '{range}' không hợp lệ. Chỉ chấp nhận: week, month, year.")
    };

    public static (DateOnly Start, DateOnly End) WeekBounds(DateOnly day)
    {
        var dow = (int)day.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var weekStart = day.AddDays(-daysFromMonday);
        return (weekStart, weekStart.AddDays(7));
    }
}
