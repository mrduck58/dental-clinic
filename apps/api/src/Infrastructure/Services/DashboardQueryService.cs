using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Application.Interfaces;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Dashboard.DashboardDateHelper;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>Đọc trực tiếp từ AppDbContext — truy vấn báo cáo/tổng hợp đa entity (Appointment, Invoice,
/// Patient, Feedback, WorkSchedule) cho nhóm Dashboard (admin). Logic chuyển verbatim từ các handler cũ.</summary>
public class DashboardQueryService(AppDbContext db) : IDashboardQueryService
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<DashboardStatsDto> GetStatsAsync(string? range, CancellationToken ct)
    {
        var normalizedRange = NormalizeRange(range);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);
        var (previousStart, previousEnd) = GetPreviousPeriodDates(normalizedRange, currentStart);

        var currentStartOffset = ToVn(currentStart);
        var currentEndOffset = ToVn(currentEnd);
        var previousStartOffset = ToVn(previousStart);
        var previousEndOffset = ToVn(previousEnd);

        var newPatientsCurrent = await db.Patients
            .CountAsync(p => p.CreatedAt >= currentStartOffset && p.CreatedAt < currentEndOffset, ct);
        var newPatientsPrevious = await db.Patients
            .CountAsync(p => p.CreatedAt >= previousStartOffset && p.CreatedAt < previousEndOffset, ct);

        var appointmentsCurrent = await CountAppointmentsAsync(currentStartOffset, currentEndOffset, ct);
        var appointmentsPrevious = await CountAppointmentsAsync(previousStartOffset, previousEndOffset, ct);

        var revenueCurrent = await SumRevenueAsync(currentStartOffset, currentEndOffset, ct);
        var revenuePrevious = await SumRevenueAsync(previousStartOffset, previousEndOffset, ct);

        return new DashboardStatsDto(
            normalizedRange,
            currentStartOffset,
            currentEndOffset,
            newPatientsCurrent,
            CalcTrendPercent(newPatientsCurrent, newPatientsPrevious),
            appointmentsCurrent,
            CalcTrendPercent(appointmentsCurrent, appointmentsPrevious),
            revenueCurrent,
            CalcTrendPercent(revenueCurrent, revenuePrevious));
    }

    private async Task<int> CountAppointmentsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>
        await db.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status != AppointmentStatus.Cancelled, ct);

    /// <summary>Doanh thu thực thu = DepositAmount (số tiền thu trên hóa đơn) của các hóa đơn đã thanh toán trong kỳ.</summary>
    private async Task<decimal> SumRevenueAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>
        await db.Invoices
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= start && i.PaymentDate < end)
            .SumAsync(i => (decimal?)i.DepositAmount, ct) ?? 0m;

    private static double CalcTrendPercent(int current, int previous) => CalcTrendPercent((decimal)current, (decimal)previous);

    private static double CalcTrendPercent(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0 : 100;
        return (double)Math.Round((current - previous) / previous * 100, 1);
    }

    public async Task<AppointmentTrendDto> GetAppointmentTrendAsync(string? range, CancellationToken ct)
    {
        var normalizedRange = NormalizeRange(range);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);

        var startOffset = ToVn(currentStart);
        var endOffset = ToVn(currentEnd);

        // Chỉ lấy cột ngày giờ hẹn (không kéo cả entity) rồi gom nhóm theo ngày (giờ VN) trong bộ nhớ —
        // khoảng thời gian tối đa 1 năm nên tập dữ liệu đủ nhỏ để xử lý an toàn, tránh rủi ro dịch
        // biểu thức .Date trên cột timestamptz sang SQL của từng provider.
        var appointmentDates = await db.Appointments
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

    public async Task<TodayAppointmentsDto> GetTodayAppointmentsAsync(int page, int pageSize, CancellationToken ct)
    {
        var clampedPage = Math.Max(page, 1);
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);

        var today = GetVietnamToday();
        var startOffset = ToVn(today);
        var endOffset = ToVn(today.AddDays(1));

        var appointmentsQuery = db.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= startOffset && a.AppointmentDate < endOffset)
            .OrderBy(a => a.AppointmentDate);

        var total = await appointmentsQuery.CountAsync(ct);
        var items = await appointmentsQuery
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .Select(a => new TodayAppointmentItemDto(
                a.Id,
                a.AppointmentDate,
                a.Patient.User.FullName ?? string.Empty,
                a.Service != null ? a.Service.Name : null,
                a.Status.ToString()))
            .ToListAsync(ct);

        return new TodayAppointmentsDto(
            items, total, clampedPage, clampedPageSize, (int)Math.Ceiling((double)total / clampedPageSize));
    }

    public async Task<RecentFeedbackDto> GetRecentFeedbackAsync(int limit, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(limit, 1, 20);

        var featuredQuery = db.Feedbacks.AsNoTracking().Where(f => f.Status == FeedbackStatus.Featured);

        var totalFeatured = await featuredQuery.CountAsync(ct);
        var averageRating = totalFeatured > 0
            ? Math.Round(await featuredQuery.AverageAsync(f => (double)f.Rating, ct), 1)
            : 0;

        var items = await featuredQuery
            .OrderByDescending(f => f.CreatedAt)
            .Take(clampedLimit)
            .Select(f => new FeedbackSummaryDto(f.Id, f.CustomerName, f.Rating, f.Comment, f.CreatedAt))
            .ToListAsync(ct);

        return new RecentFeedbackDto(items, averageRating, totalFeatured);
    }

    public async Task<ServiceDistributionDto> GetServiceDistributionAsync(string? range, int topN, CancellationToken ct)
    {
        var normalizedRange = NormalizeRange(range);
        var clampedTopN = Math.Clamp(topN, 1, 20);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);
        var startOffset = ToVn(currentStart);
        var endOffset = ToVn(currentEnd);

        var grouped = await db.Appointments
            .Where(a => a.AppointmentDate >= startOffset && a.AppointmentDate < endOffset
                        && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = grouped.Sum(g => g.Count);
        if (total == 0)
            return new ServiceDistributionDto(normalizedRange, 0, []);

        var serviceIds = grouped.Where(g => g.ServiceId.HasValue).Select(g => g.ServiceId!.Value).ToList();
        var namesById = await db.Services
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var ranked = grouped.OrderByDescending(g => g.Count).ToList();
        var top = ranked.Take(clampedTopN).ToList();
        var otherCount = ranked.Skip(clampedTopN).Sum(g => g.Count);

        var items = top
            .Select(g => new ServiceDistributionItemDto(
                g.ServiceId,
                g.ServiceId.HasValue ? namesById.GetValueOrDefault(g.ServiceId.Value) : null,
                g.Count,
                Math.Round((double)g.Count / total * 100, 1)))
            .ToList();

        if (otherCount > 0)
            items.Add(new ServiceDistributionItemDto(null, null, otherCount, Math.Round((double)otherCount / total * 100, 1)));

        return new ServiceDistributionDto(normalizedRange, total, items);
    }

    public async Task<WeeklyScheduleDto> GetWeeklyScheduleAsync(DateOnly? date, CancellationToken ct)
    {
        var today = GetVietnamToday();
        var selectedDate = date ?? today;

        var dow = (int)selectedDate.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var weekStart = selectedDate.AddDays(-daysFromMonday);

        var week = Enumerable.Range(0, 7)
            .Select(weekStart.AddDays)
            .Select(d => new CalendarDayDto(d, d == today))
            .ToList();

        var shifts = await db.WorkSchedules
            .AsNoTracking()
            .Where(s => s.Date == selectedDate && s.Type == "dentist" && !s.IsHoliday)
            .ToListAsync(ct);

        var staffNames = shifts.Select(s => s.StaffName).Distinct().ToList();

        var dentistsByName = await db.DentistProfiles
            .AsNoTracking()
            .Include(d => d.Employee).ThenInclude(e => e.User)
            .Where(d => staffNames.Contains(d.Employee.User.FullName ?? string.Empty))
            .ToDictionaryAsync(d => d.Employee.User.FullName ?? string.Empty, d => d, ct);

        var dayStart = ToVn(selectedDate);
        var dayEnd = ToVn(selectedDate.AddDays(1));
        var busyDentistIds = (await db.Appointments
                .Where(a => a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd
                            && a.Status == AppointmentStatus.InProgress)
                .Select(a => a.DentistId)
                .ToListAsync(ct))
            .ToHashSet();

        return new WeeklyScheduleDto(
            selectedDate,
            week,
            BuildShiftEntries(shifts, "morning", dentistsByName, busyDentistIds),
            BuildShiftEntries(shifts, "afternoon", dentistsByName, busyDentistIds));
    }

    private static List<ShiftEntryDto> BuildShiftEntries(
        IEnumerable<Domain.Entities.WorkSchedule> shifts,
        string shift,
        IReadOnlyDictionary<string, Domain.Entities.DentistProfile> dentistsByName,
        IReadOnlySet<Guid> busyDentistIds) => shifts
        .Where(s => s.Shift == shift)
        .OrderBy(s => s.StaffName)
        .Select(s =>
        {
            dentistsByName.TryGetValue(s.StaffName, out var dentist);
            var isBusy = dentist != null && busyDentistIds.Contains(dentist.Id);
            return new ShiftEntryDto(
                s.StaffName,
                dentist?.Specialization,
                dentist?.Employee.ProfilePictureUrl,
                s.Room,
                s.RoomColor,
                isBusy);
        })
        .ToList();
}
