using DentalClinic.API.Application.UseCases.AiAnalytics;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
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

        var user = User.Create($"patient-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", "Patient");
        _db.Users.Add(user);
        _patient = Patient.Create("Bệnh nhân Test", new DateOnly(1990, 1, 1), "Nam", user.Id);
        _db.Patients.Add(_patient);
        await _db.SaveChangesAsync();

        _handler = new GetAiAnalyticsHandler(_db);
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

        var result = await _handler.HandleAsync(rangeDays: 14);

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

        var result = await _handler.HandleAsync();

        result.TotalConversations.Should().Be(1);
        result.TotalUserMessages.Should().Be(1);
        result.TotalMessages.Should().Be(4);
        result.SuggestBookingCount.Should().Be(1);
        result.BookingActionCount.Should().Be(1);
    }

    [Test]
    public async Task HandleAsync_NoData_ReturnsZeroedResultWithoutThrowing()
    {
        var result = await _handler.HandleAsync();

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

        var result = await _handler.HandleAsync(rangeDays: null);

        result.RangeDays.Should().BeNull();
        result.UsageByFeature.Should().ContainSingle(f => f.Feature == "PatientSummary");
    }
}
