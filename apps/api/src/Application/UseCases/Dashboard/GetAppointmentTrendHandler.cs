using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Dashboard.DashboardDateHelper;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetAppointmentTrendQuery(string? Range) : IRequest<AppointmentTrendDto>;

public class GetAppointmentTrendHandler(AppDbContext dbContext) : IRequestHandler<GetAppointmentTrendQuery, AppointmentTrendDto>
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<AppointmentTrendDto> Handle(GetAppointmentTrendQuery query, CancellationToken ct)
    {
        var normalizedRange = NormalizeRange(query.Range);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);

        var startOffset = ToVn(currentStart);
        var endOffset = ToVn(currentEnd);

        // Chỉ lấy cột ngày giờ hẹn (không kéo cả entity) rồi gom nhóm theo ngày (giờ VN) trong bộ nhớ —
        // khoảng thời gian tối đa 1 năm nên tập dữ liệu đủ nhỏ để xử lý an toàn, tránh rủi ro dịch
        // biểu thức .Date trên cột timestamptz sang SQL của từng provider.
        var appointmentDates = await dbContext.Appointments
            .Where(a => a.AppointmentDate >= startOffset && a.AppointmentDate < endOffset
                        && a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.AppointmentDate)
            .ToListAsync(ct);

        var countsByDate = appointmentDates
            .GroupBy(d => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(d, VietnamTz).DateTime))
            .ToDictionary(g => g.Key, g => g.Count());

        List<AppointmentTrendPointDto> points = normalizedRange switch
        {
            "week"  => BuildDailyBuckets(currentStart, 7, countsByDate),
            "month" => BuildWeeklyBucketsInMonth(currentStart, countsByDate),
            _       => BuildMonthlyBucketsInYear(currentStart.Year, countsByDate)
        };

        return new AppointmentTrendDto(normalizedRange, points);
    }

    private static List<AppointmentTrendPointDto> BuildDailyBuckets(
        DateOnly start, int days, IReadOnlyDictionary<DateOnly, int> counts)
    {
        var points = new List<AppointmentTrendPointDto>(days);
        for (var i = 0; i < days; i++)
        {
            var day = start.AddDays(i);
            points.Add(new AppointmentTrendPointDto(ToVn(day), ToVn(day.AddDays(1)), counts.GetValueOrDefault(day, 0)));
        }
        return points;
    }

    private static List<AppointmentTrendPointDto> BuildWeeklyBucketsInMonth(
        DateOnly monthStart, IReadOnlyDictionary<DateOnly, int> counts)
    {
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        var points = new List<AppointmentTrendPointDto>();
        for (var dayOffset = 0; dayOffset < daysInMonth; dayOffset += 7)
        {
            var bucketStart = monthStart.AddDays(dayOffset);
            var bucketLength = Math.Min(7, daysInMonth - dayOffset);
            var bucketEnd = bucketStart.AddDays(bucketLength);

            var count = 0;
            for (var d = bucketStart; d < bucketEnd; d = d.AddDays(1))
                count += counts.GetValueOrDefault(d, 0);

            points.Add(new AppointmentTrendPointDto(ToVn(bucketStart), ToVn(bucketEnd), count));
        }
        return points;
    }

    private static List<AppointmentTrendPointDto> BuildMonthlyBucketsInYear(
        int year, IReadOnlyDictionary<DateOnly, int> counts)
    {
        var points = new List<AppointmentTrendPointDto>(12);
        for (var month = 1; month <= 12; month++)
        {
            var bucketStart = new DateOnly(year, month, 1);
            var bucketEnd = bucketStart.AddMonths(1);

            var count = 0;
            for (var d = bucketStart; d < bucketEnd; d = d.AddDays(1))
                count += counts.GetValueOrDefault(d, 0);

            points.Add(new AppointmentTrendPointDto(ToVn(bucketStart), ToVn(bucketEnd), count));
        }
        return points;
    }
}
