using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.AiAnalytics;

public record AiFeatureUsageDto(
    string Feature, int TotalCalls, int SuccessCount, int FailureCount, double AvgDurationMs);

public record AiDailyUsageDto(DateOnly Date, int Calls, int Failures);

public record AiAnalyticsDto(
    int? RangeDays,
    int TotalConversations,
    int TotalMessages,
    int TotalUserMessages,
    int SuggestBookingCount,
    int BookingActionCount,
    List<AiFeatureUsageDto> UsageByFeature,
    List<AiDailyUsageDto> DailyUsage);

/// <summary>
/// Tổng hợp số liệu vận hành cho toàn bộ tính năng AI trong hệ thống (chatbot, tóm tắt bệnh án, soạn
/// nội dung marketing), phục vụ trang thống kê cho Admin. Không hiển thị nội dung hội thoại/prompt —
/// chỉ số liệu tổng hợp (số lượng, tỷ lệ, thời gian phản hồi) để bảo vệ thông tin bệnh nhân.
/// </summary>
public class GetAiAnalyticsHandler(AppDbContext dbContext)
{
    /// <summary><paramref name="rangeDays"/> = null nghĩa là lấy TẤT CẢ dữ liệu từ trước tới nay
    /// (không lọc theo thời gian) — dùng cho tùy chọn "Tất cả" trên UI.</summary>
    public async Task<AiAnalyticsDto> HandleAsync(int? rangeDays = 14, CancellationToken ct = default)
    {
        if (rangeDays.HasValue)
        {
            rangeDays = Math.Clamp(rangeDays.Value, 1, 90);
        }
        var since = rangeDays.HasValue
            ? DateTimeOffset.UtcNow.AddDays(-rangeDays.Value)
            : DateTimeOffset.MinValue;

        var totalConversations = await dbContext.ChatConversations.CountAsync(c => c.CreatedAt >= since, ct);

        var assistantMessages = await dbContext.ChatMessages
            .Where(m => m.CreatedAt >= since && m.Role == "assistant")
            .ToListAsync(ct);
        var totalUserMessages = await dbContext.ChatMessages
            .CountAsync(m => m.CreatedAt >= since && m.Role == "user", ct);
        var totalMessages = assistantMessages.Count + totalUserMessages;

        var usageLogs = await dbContext.AiUsageLogs.Where(l => l.CreatedAt >= since).ToListAsync(ct);

        var usageByFeature = usageLogs
            .GroupBy(l => l.Feature)
            .Select(g => new AiFeatureUsageDto(
                g.Key,
                g.Count(),
                g.Count(x => x.Success),
                g.Count(x => !x.Success),
                Math.Round(g.Average(x => x.DurationMs), 0)))
            .OrderByDescending(x => x.TotalCalls)
            .ToList();

        var dailyUsage = usageLogs
            .GroupBy(l => DateOnly.FromDateTime(l.CreatedAt.UtcDateTime))
            .Select(g => new AiDailyUsageDto(g.Key, g.Count(), g.Count(x => !x.Success)))
            .OrderBy(x => x.Date)
            .ToList();

        return new AiAnalyticsDto(
            rangeDays,
            totalConversations,
            totalMessages,
            totalUserMessages,
            assistantMessages.Count(m => m.SuggestBooking),
            assistantMessages.Count(m => m.BookingActionTaken),
            usageByFeature,
            dailyUsage);
    }
}
