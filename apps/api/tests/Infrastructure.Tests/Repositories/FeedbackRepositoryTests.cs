using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class FeedbackRepositoryTests
{
    private AppDbContext _db = null!;
    private FeedbackRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new FeedbackRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>AddAsync phải lưu feedback vào DB.</summary>
    [Test]
    public async Task AddAsync_ValidFeedback_PersistsToDatabase()
    {
        var feedback = Feedback.Create("Nguyễn Văn A", 5, "Rất tốt");

        await _sut.AddAsync(feedback);

        (await _db.Feedbacks.CountAsync()).Should().Be(1);
    }

    /// <summary>GetAllAsync phải trả về tất cả feedback, mới nhất trước.</summary>
    [Test]
    public async Task GetAllAsync_MultipleFeedbacks_OrdersByCreatedAtDescending()
    {
        var older = Feedback.Create("A", 4, "Ổn");
        var newer = Feedback.Create("B", 5, "Tuyệt vời");
        _db.Feedbacks.Add(older);
        await _db.SaveChangesAsync();
        _db.Feedbacks.Add(newer);
        await _db.SaveChangesAsync();
        typeof(Feedback).GetProperty(nameof(Feedback.CreatedAt))!.SetValue(older, DateTimeOffset.UtcNow.AddHours(-2));
        typeof(Feedback).GetProperty(nameof(Feedback.CreatedAt))!.SetValue(newer, DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync();

        var result = (await _sut.GetAllAsync()).ToList();

        result[0].Id.Should().Be(newer.Id);
        result[1].Id.Should().Be(older.Id);
    }

    /// <summary>GetByIdAsync với id tồn tại phải trả về đúng feedback.</summary>
    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsFeedback()
    {
        var feedback = Feedback.Create("Trần Thị B", 3, "Bình thường");
        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(feedback.Id);

        result.Should().NotBeNull();
        result!.CustomerName.Should().Be("Trần Thị B");
    }

    /// <summary>GetByIdAsync với id không tồn tại phải trả về null.</summary>
    [Test]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    /// <summary>UpdateAsync phải lưu lại thay đổi trạng thái (ví dụ sau khi reply).</summary>
    [Test]
    public async Task UpdateAsync_ModifiedFeedback_PersistsChanges()
    {
        var feedback = Feedback.Create("Lê Văn C", 5, "Xuất sắc");
        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        feedback.Reply("Cảm ơn bạn đã đánh giá!");
        await _sut.UpdateAsync(feedback);

        var reloaded = await _db.Feedbacks.FindAsync(feedback.Id);
        reloaded!.ReplyText.Should().Be("Cảm ơn bạn đã đánh giá!");
    }
}
