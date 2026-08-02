using DentalClinic.API.Application.UseCases.Dashboard;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetRecentFeedbackHandlerTests
{
    private AppDbContext _db = null!;
    private GetRecentFeedbackHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetRecentFeedbackHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Chỉ đánh giá ở trạng thái Featured mới được trả về, bỏ qua Pending/Hidden.</summary>
    [Test]
    public async Task Handle_OnlyReturnsFeaturedFeedback()
    {
        var featured = Feedback.Create("Nguyễn Thị Thu Hà", 5, "Rất hài lòng");
        featured.Feature();
        var pending = Feedback.Create("Trần Hoàng Nam", 4, "Tạm ổn");
        var hidden = Feedback.Create("Phạm Minh Anh", 1, "Không hài lòng");
        hidden.Hide();
        _db.Feedbacks.AddRange(featured, pending, hidden);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRecentFeedbackQuery(5), CancellationToken.None);

        result.TotalFeaturedCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.CustomerName == "Nguyễn Thị Thu Hà");
    }

    [Test]
    public async Task Handle_ComputesAverageRatingAcrossFeaturedOnly()
    {
        var f1 = Feedback.Create("KH 1", 5, "Tốt");
        f1.Feature();
        var f2 = Feedback.Create("KH 2", 3, "Bình thường");
        f2.Feature();
        _db.Feedbacks.AddRange(f1, f2);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRecentFeedbackQuery(5), CancellationToken.None);

        result.AverageRating.Should().Be(4);
    }

    [Test]
    public async Task Handle_RespectsLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            var fb = Feedback.Create($"KH {i}", 5, "Tốt");
            fb.Feature();
            _db.Feedbacks.Add(fb);
        }
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetRecentFeedbackQuery(2), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalFeaturedCount.Should().Be(5);
    }

    [Test]
    public async Task Handle_NoFeaturedFeedback_ReturnsZeroAverage()
    {
        var result = await _handler.Handle(new GetRecentFeedbackQuery(5), CancellationToken.None);

        result.AverageRating.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
