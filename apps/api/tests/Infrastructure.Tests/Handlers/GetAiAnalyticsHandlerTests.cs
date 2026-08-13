using DentalClinic.API.Application.UseCases.AiAnalytics;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetAiAnalyticsHandlerTests
{
    private AppDbContext _db = null!;
    private GetAiAnalyticsHandler _handler = null!;
    private Patient _patient = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var user = User.Create($"patient-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: "Bệnh nhân Test");
        _db.Users.Add(user);
        _patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam");
        _db.Patients.Add(_patient);
        await _db.SaveChangesAsync();

        _handler = new GetAiAnalyticsHandler(
            new ChatConversationRepository(_db), new ChatMessageRepository(_db), new AiUsageLogRepository(_db));
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Chỉ tính usage log/tin nhắn NẰM TRONG khoảng rangeDays — dữ liệu cũ hơn phải bị loại
    /// khỏi thống kê để phản ánh đúng xu hướng gần đây.</summary>
    [Test]
    public async Task HandleAsync_OnlyCountsDataWithinRange()
    {
        _db.AiUsageLogs.Add(AiUsageLog.Create("ChatBot", true, 500, null));
        _db.AiUsageLogs.Add(AiUsageLog.Create("ChatBot", false, 300, "timeout"));
        await _db.SaveChangesAsync();

        // Log quá cũ (ngoài rangeDays=14) không được tính vào thống kê.
        var oldLog = AiUsageLog.Create("PatientSummary", true, 200, null);
        _db.AiUsageLogs.Add(oldLog);
        await _db.SaveChangesAsync();
        _db.Entry(oldLog).Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow.AddDays(-30);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAiAnalyticsQuery(RangeDays: 14), CancellationToken.None);

        result.UsageByFeature.Should().ContainSingle(f => f.Feature == "ChatBot");
        var chatFeature = result.UsageByFeature.Single(f => f.Feature == "ChatBot");
        chatFeature.TotalCalls.Should().Be(2);
        chatFeature.SuccessCount.Should().Be(1);
        chatFeature.FailureCount.Should().Be(1);
        chatFeature.AvgDurationMs.Should().Be(400);

        result.UsageByFeature.Should().NotContain(f => f.Feature == "PatientSummary");
    }

    /// <summary>Tỷ lệ gợi ý đặt lịch / đặt-hủy lịch thành công phải đếm đúng theo cờ đã lưu trên
    /// ChatMessage, không đếm nhầm tin nhắn của bệnh nhân (role "user").</summary>
    [Test]
    public async Task HandleAsync_CountsSuggestBookingAndBookingActionFromAssistantMessagesOnly()
    {
        var conversation = ChatConversation.Create(_patient.Id);
        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();

        _db.ChatMessages.Add(ChatMessage.Create(conversation.Id, "user", "Tôi bị đau răng"));
        _db.ChatMessages.Add(ChatMessage.Create(
            conversation.Id, "assistant", "Bạn nên đến khám", suggestBooking: true));
        _db.ChatMessages.Add(ChatMessage.Create(
            conversation.Id, "assistant", "Đã đặt lịch thành công", bookingActionTaken: true));
        _db.ChatMessages.Add(ChatMessage.Create(conversation.Id, "assistant", "Giờ làm việc 8h-20h"));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAiAnalyticsQuery(), CancellationToken.None);

        result.TotalConversations.Should().Be(1);
        result.TotalUserMessages.Should().Be(1);
        result.TotalMessages.Should().Be(4);
        result.SuggestBookingCount.Should().Be(1);
        result.BookingActionCount.Should().Be(1);
    }

    [Test]
    public async Task HandleAsync_NoData_ReturnsZeroedResultWithoutThrowing()
    {
        var result = await _handler.Handle(new GetAiAnalyticsQuery(), CancellationToken.None);

        result.TotalConversations.Should().Be(0);
        result.TotalMessages.Should().Be(0);
        result.UsageByFeature.Should().BeEmpty();
        result.DailyUsage.Should().BeEmpty();
    }

    /// <summary>rangeDays = null (tùy chọn "Tất cả") phải lấy TOÀN BỘ dữ liệu, kể cả log rất cũ mà một
    /// khoảng ngày cụ thể (vd. 90) sẽ loại bỏ.</summary>
    [Test]
    public async Task HandleAsync_RangeDaysNull_IncludesAllDataRegardlessOfAge()
    {
        var oldLog = AiUsageLog.Create("PatientSummary", true, 200, null);
        _db.AiUsageLogs.Add(oldLog);
        await _db.SaveChangesAsync();
        _db.Entry(oldLog).Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow.AddYears(-2);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAiAnalyticsQuery(RangeDays: null), CancellationToken.None);

        result.RangeDays.Should().BeNull();
        result.UsageByFeature.Should().ContainSingle(f => f.Feature == "PatientSummary");
    }

    /// <summary>rangeDays dưới mức tối thiểu (0 hoặc âm) phải bị clamp về 1 — không cho phép khoảng
    /// thời gian bằng 0 hoặc ngược ngày.</summary>
    [Test]
    public async Task HandleAsync_RangeDaysBelowMin_ClampedToOne()
    {
        var result = await _handler.Handle(new GetAiAnalyticsQuery(RangeDays: 0), CancellationToken.None);

        result.RangeDays.Should().Be(1);
    }

    /// <summary>rangeDays vượt mức tối đa (90) phải bị clamp về 90 để tránh truy vấn quá nhiều dữ liệu
    /// lịch sử.</summary>
    [Test]
    public async Task HandleAsync_RangeDaysAboveMax_ClampedTo90()
    {
        var result = await _handler.Handle(new GetAiAnalyticsQuery(RangeDays: 365), CancellationToken.None);

        result.RangeDays.Should().Be(90);
    }

    /// <summary>DailyUsage phải gom nhóm theo từng ngày (UTC date) và sắp xếp tăng dần theo ngày, với
    /// số lượt gọi/thất bại đúng cho từng ngày.</summary>
    [Test]
    public async Task HandleAsync_DailyUsage_GroupedAndOrderedByDateAscending()
    {
        var today = AiUsageLog.Create("ChatBot", true, 100, null);
        var yesterdayOk = AiUsageLog.Create("ChatBot", true, 100, null);
        var yesterdayFail = AiUsageLog.Create("ChatBot", false, 100, "err");
        _db.AiUsageLogs.AddRange(today, yesterdayOk, yesterdayFail);
        await _db.SaveChangesAsync();
        _db.Entry(yesterdayOk).Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        _db.Entry(yesterdayFail).Property("CreatedAt").CurrentValue = DateTimeOffset.UtcNow.AddDays(-1);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAiAnalyticsQuery(RangeDays: 14), CancellationToken.None);

        result.DailyUsage.Should().HaveCount(2);
        result.DailyUsage.Select(d => d.Date).Should().BeInAscendingOrder();
        var yesterdayEntry = result.DailyUsage[0];
        yesterdayEntry.Calls.Should().Be(2);
        yesterdayEntry.Failures.Should().Be(1);
        var todayEntry = result.DailyUsage[1];
        todayEntry.Calls.Should().Be(1);
        todayEntry.Failures.Should().Be(0);
    }

    /// <summary>UsageByFeature phải được sắp xếp giảm dần theo tổng số lượt gọi (TotalCalls), để tính
    /// năng được dùng nhiều nhất hiển thị đầu tiên trên bảng thống kê.</summary>
    [Test]
    public async Task HandleAsync_UsageByFeature_OrderedByTotalCallsDescending()
    {
        _db.AiUsageLogs.Add(AiUsageLog.Create("RarelyUsed", true, 100, null));
        _db.AiUsageLogs.Add(AiUsageLog.Create("PopularFeature", true, 100, null));
        _db.AiUsageLogs.Add(AiUsageLog.Create("PopularFeature", true, 100, null));
        _db.AiUsageLogs.Add(AiUsageLog.Create("PopularFeature", false, 100, "err"));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAiAnalyticsQuery(), CancellationToken.None);

        result.UsageByFeature.Should().HaveCount(2);
        result.UsageByFeature[0].Feature.Should().Be("PopularFeature");
        result.UsageByFeature[0].TotalCalls.Should().Be(3);
        result.UsageByFeature[1].Feature.Should().Be("RarelyUsed");
        result.UsageByFeature[1].TotalCalls.Should().Be(1);
    }
}
