using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Dashboard;

#region DTOs & Requests

/// <summary>3 chỉ số chính của dashboard (bệnh nhân mới, lịch hẹn, doanh thu) so với kỳ trước đó.</summary>
public record DashboardStatsDto(
    string Range,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int NewPatientsCount,
    double NewPatientsTrendPercent,
    int AppointmentsCount,
    double AppointmentsTrendPercent,
    decimal RevenueAmount,
    double RevenueTrendPercent);

public record AppointmentTrendPointDto(DateTimeOffset PeriodStart, DateTimeOffset PeriodEnd, int Count);

public record AppointmentTrendDto(string Range, IReadOnlyList<AppointmentTrendPointDto> Points);

/// <summary>ServiceId/ServiceName null đại diện cho nhóm "các dịch vụ còn lại" khi vượt quá topN.</summary>
public record ServiceDistributionItemDto(Guid? ServiceId, string? ServiceName, int Count, double Percentage);

public record ServiceDistributionDto(string Range, int TotalAppointments, IReadOnlyList<ServiceDistributionItemDto> Items);

public record TodayAppointmentItemDto(
    Guid Id,
    DateTimeOffset AppointmentDate,
    string PatientName,
    string? ServiceName,
    string Status);

public record TodayAppointmentsDto(
    IReadOnlyList<TodayAppointmentItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public record CalendarDayDto(DateOnly Date, bool IsToday);

public record ShiftEntryDto(
    string StaffName,
    string? Specialization,
    string? ProfilePictureUrl,
    string Room,
    string RoomColor,
    bool IsBusy);

public record WeeklyScheduleDto(
    DateOnly SelectedDate,
    IReadOnlyList<CalendarDayDto> Week,
    IReadOnlyList<ShiftEntryDto> MorningShift,
    IReadOnlyList<ShiftEntryDto> AfternoonShift);

public record FeedbackSummaryDto(Guid Id, string CustomerName, int Rating, string Comment, DateTimeOffset CreatedAt);

public record RecentFeedbackDto(
    IReadOnlyList<FeedbackSummaryDto> Items,
    double AverageRating,
    int TotalFeaturedCount);

#endregion

/// <summary>
/// Tổng hợp dữ liệu cho trang Dashboard Admin (tổng quan vận hành).
/// Đọc trực tiếp từ AppDbContext vì đây là các truy vấn báo cáo/tổng hợp đa entity
/// (Appointment, Invoice, Patient, WorkSchedule, Feedback) — cùng phong cách với
/// DentistDashboardHandler và InvoiceHandler.
/// </summary>
public class DashboardHandler(AppDbContext dbContext)
{
    private static readonly TimeZoneInfo VietnamTz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    // ── 1. Stats grid (3 chỉ số + % so với kỳ trước) ────────────────────────

    public async Task<DashboardStatsDto> GetStatsAsync(string? range, CancellationToken ct = default)
    {
        var normalizedRange = NormalizeRange(range);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);
        var (previousStart, previousEnd) = GetPreviousPeriodDates(normalizedRange, currentStart);

        var currentStartOffset = ToVn(currentStart);
        var currentEndOffset = ToVn(currentEnd);
        var previousStartOffset = ToVn(previousStart);
        var previousEndOffset = ToVn(previousEnd);

        var newPatientsCurrent = await dbContext.Patients
            .CountAsync(p => p.CreatedAt >= currentStartOffset && p.CreatedAt < currentEndOffset, ct);
        var newPatientsPrevious = await dbContext.Patients
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
        await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status != AppointmentStatus.Cancelled, ct);

    /// <summary>Doanh thu thực thu = DepositAmount (số tiền thu trên hóa đơn) của các hóa đơn đã thanh toán trong kỳ.</summary>
    private async Task<decimal> SumRevenueAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>
        await dbContext.Invoices
            .Where(i => i.Status == PaymentStatus.Paid && i.PaymentDate >= start && i.PaymentDate < end)
            .SumAsync(i => (decimal?)i.DepositAmount, ct) ?? 0m;

    // ── 2. Appointment trend (biểu đồ cột) ──────────────────────────────────

    public async Task<AppointmentTrendDto> GetAppointmentTrendAsync(string? range, CancellationToken ct = default)
    {
        var normalizedRange = NormalizeRange(range);
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

    // ── 3. Service distribution (biểu đồ tròn) ──────────────────────────────

    public async Task<ServiceDistributionDto> GetServiceDistributionAsync(
        string? range, int topN, CancellationToken ct = default)
    {
        var normalizedRange = NormalizeRange(range);
        var clampedTopN = Math.Clamp(topN, 1, 20);
        var today = GetVietnamToday();
        var (currentStart, currentEnd) = GetCurrentPeriodDates(normalizedRange, today);
        var startOffset = ToVn(currentStart);
        var endOffset = ToVn(currentEnd);

        var grouped = await dbContext.Appointments
            .Where(a => a.AppointmentDate >= startOffset && a.AppointmentDate < endOffset
                        && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var total = grouped.Sum(g => g.Count);
        if (total == 0)
            return new ServiceDistributionDto(normalizedRange, 0, []);

        var serviceIds = grouped.Where(g => g.ServiceId.HasValue).Select(g => g.ServiceId!.Value).ToList();
        var namesById = await dbContext.Services
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

    // ── 4. Lịch hẹn hôm nay (bảng, có phân trang) ───────────────────────────

    public async Task<TodayAppointmentsDto> GetTodayAppointmentsAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var clampedPage = Math.Max(page, 1);
        var clampedPageSize = Math.Clamp(pageSize, 1, 100);

        var today = GetVietnamToday();
        var startOffset = ToVn(today);
        var endOffset = ToVn(today.AddDays(1));

        var query = dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= startOffset && a.AppointmentDate < endOffset)
            .OrderBy(a => a.AppointmentDate);

        var total = await query.CountAsync(ct);
        var items = await query
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

    // ── 5. Lịch vận hành & ca trực trong tuần ────────────────────────────────

    public async Task<WeeklyScheduleDto> GetWeeklyScheduleAsync(DateOnly? date, CancellationToken ct = default)
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

        var shifts = await dbContext.WorkSchedules
            .AsNoTracking()
            .Where(s => s.Date == selectedDate && s.Type == "dentist" && !s.IsHoliday)
            .ToListAsync(ct);

        var staffNames = shifts.Select(s => s.StaffName).Distinct().ToList();

        var dentistsByName = await dbContext.Dentists
            .AsNoTracking()
            .Include(d => d.User)
            .Where(d => staffNames.Contains(d.User.FullName ?? string.Empty))
            .ToDictionaryAsync(d => d.User.FullName ?? string.Empty, d => d, ct);

        var dayStart = ToVn(selectedDate);
        var dayEnd = ToVn(selectedDate.AddDays(1));
        var busyDentistIds = (await dbContext.Appointments
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
        IReadOnlyDictionary<string, Domain.Entities.Dentist> dentistsByName,
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
                dentist?.User?.ProfilePictureUrl,
                s.Room,
                s.RoomColor,
                isBusy);
        })
        .ToList();

    // ── 6. Đánh giá gần đây từ khách hàng ─────────────────────────────────────

    public async Task<RecentFeedbackDto> GetRecentFeedbackAsync(int limit, CancellationToken ct = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 20);

        var featuredQuery = dbContext.Feedbacks.AsNoTracking().Where(f => f.Status == FeedbackStatus.Featured);

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

    // ── Helpers dùng chung ───────────────────────────────────────────────────

    private static string NormalizeRange(string? range)
    {
        var normalized = range?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "week" or "month" or "year" => normalized,
            null or "" => "week",
            _ => throw new ValidationException($"Khoảng thời gian '{range}' không hợp lệ. Chỉ chấp nhận: week, month, year.")
        };
    }

    private static DateOnly GetVietnamToday()
    {
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        return DateOnly.FromDateTime(vietnamNow);
    }

    /// <summary>
    /// Nửa đêm (giờ VN) của một ngày, quy về UTC — Npgsql chỉ chấp nhận DateTimeOffset
    /// với Offset=0 khi ghi vào cột "timestamp with time zone".
    /// </summary>
    private static DateTimeOffset ToVn(DateOnly date) =>
        new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTz.BaseUtcOffset).ToUniversalTime();

    private static (DateOnly Start, DateOnly End) GetCurrentPeriodDates(string range, DateOnly today) => range switch
    {
        "week" => WeekBounds(today),
        "month" => (new DateOnly(today.Year, today.Month, 1), new DateOnly(today.Year, today.Month, 1).AddMonths(1)),
        "year" => (new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 1, 1).AddYears(1)),
        _ => throw new ValidationException($"Khoảng thời gian '{range}' không hợp lệ. Chỉ chấp nhận: week, month, year.")
    };

    private static (DateOnly Start, DateOnly End) GetPreviousPeriodDates(string range, DateOnly currentStart) => range switch
    {
        "week" => (currentStart.AddDays(-7), currentStart),
        "month" => (currentStart.AddMonths(-1), currentStart),
        "year" => (currentStart.AddYears(-1), currentStart),
        _ => throw new ValidationException($"Khoảng thời gian '{range}' không hợp lệ. Chỉ chấp nhận: week, month, year.")
    };

    private static (DateOnly Start, DateOnly End) WeekBounds(DateOnly day)
    {
        var dow = (int)day.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var weekStart = day.AddDays(-daysFromMonday);
        return (weekStart, weekStart.AddDays(7));
    }

    private static double CalcTrendPercent(int current, int previous) => CalcTrendPercent((decimal)current, (decimal)previous);

    private static double CalcTrendPercent(decimal current, decimal previous)
    {
        if (previous == 0) return current == 0 ? 0 : 100;
        return (double)Math.Round((current - previous) / previous * 100, 1);
    }
}
