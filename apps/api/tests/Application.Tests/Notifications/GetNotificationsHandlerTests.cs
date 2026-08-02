using DentalClinic.API.Application.UseCases.Notifications;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Notifications;

[TestFixture]
public class GetNotificationsHandlerTests
{
    private INotificationRepository _repo = null!;
    private GetNotificationsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<INotificationRepository>();
        _handler = new GetNotificationsHandler(_repo);
    }

    /// <summary>Kết quả trả về phải chỉ chứa thông báo của người dùng, kèm tổng số chưa đọc.</summary>
    [Test]
    public async Task HandleAsync_ValidQuery_ReturnsMappedPagedResult()
    {
        var userId = Guid.NewGuid();
        var notification = Notification.Create(userId, "system", "high", "Tiêu đề", "Nội dung");
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Notification> { notification }, 1));
        _repo.GetUnreadCountAsync(userId, Arg.Any<CancellationToken>()).Returns(3);

        var result = await _handler.Handle(new GetNotificationsQuery(userId), CancellationToken.None);

        result.Items.Should().ContainSingle(n => n.Title == "Tiêu đề");
        result.UnreadCount.Should().Be(3);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
    }

    /// <summary>PageSize vượt quá 100 phải bị giới hạn về 100 trước khi truy vấn.</summary>
    [Test]
    public async Task HandleAsync_PageSizeAboveMax_ClampsTo100()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        await _handler.Handle(new GetNotificationsQuery(userId, PageSize: 500), CancellationToken.None);

        await _repo.Received(1).GetPagedAsync(userId, null, null, null, null, 1, 100, Arg.Any<CancellationToken>());
    }

    /// <summary>PageSize dưới 1 và Page dưới 1 phải được đưa về giá trị tối thiểu hợp lệ (1).</summary>
    [Test]
    public async Task HandleAsync_PageAndPageSizeBelowMinimum_ClampsToOne()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 1, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        await _handler.Handle(new GetNotificationsQuery(userId, Page: -5, PageSize: 0), CancellationToken.None);

        await _repo.Received(1).GetPagedAsync(userId, null, null, null, null, 1, 1, Arg.Any<CancellationToken>());
    }

    /// <summary>Tổng số trang phải được tính đúng theo tổng số bản ghi và kích thước trang.</summary>
    [Test]
    public async Task HandleAsync_ComputesTotalPagesCorrectly()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 25));

        var result = await _handler.Handle(new GetNotificationsQuery(userId), CancellationToken.None);

        result.TotalPages.Should().Be(3); // ceil(25/10)
    }

    /// <summary>PageSize đúng bằng giới hạn trên (100) phải được giữ nguyên, không bị điều chỉnh.</summary>
    [Test]
    public async Task HandleAsync_PageSizeExactlyAtUpperBoundary_IsNotClamped()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 100, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        await _handler.Handle(new GetNotificationsQuery(userId, PageSize: 100), CancellationToken.None);

        await _repo.Received(1).GetPagedAsync(userId, null, null, null, null, 1, 100, Arg.Any<CancellationToken>());
    }

    /// <summary>PageSize đúng bằng giới hạn dưới (1) phải được giữ nguyên, không bị điều chỉnh.</summary>
    [Test]
    public async Task HandleAsync_PageSizeExactlyAtLowerBoundary_IsNotClamped()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 1, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        await _handler.Handle(new GetNotificationsQuery(userId, PageSize: 1), CancellationToken.None);

        await _repo.Received(1).GetPagedAsync(userId, null, null, null, null, 1, 1, Arg.Any<CancellationToken>());
    }

    /// <summary>Các filter Type, Priority, IsRead, Search phải được truyền nguyên vẹn xuống repository.</summary>
    [Test]
    public async Task HandleAsync_AllFiltersProvided_PassesThemThroughToRepository()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, "appointment", "high", true, "khám", 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        await _handler.Handle(new GetNotificationsQuery(userId, "appointment", "high", true, "khám"), CancellationToken.None);

        await _repo.Received(1).GetPagedAsync(userId, "appointment", "high", true, "khám", 1, 10, Arg.Any<CancellationToken>());
    }

    /// <summary>IsRead = false phải được truyền đúng (không bị nhầm với null/mặc định).</summary>
    [Test]
    public async Task HandleAsync_IsReadFalse_PassesFalseThroughToRepository()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, false, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        await _handler.Handle(new GetNotificationsQuery(userId, IsRead: false), CancellationToken.None);

        await _repo.Received(1).GetPagedAsync(userId, null, null, false, null, 1, 10, Arg.Any<CancellationToken>());
    }

    /// <summary>Không có bản ghi nào (TotalCount = 0) phải cho ra TotalPages = 0, không lỗi chia.</summary>
    [Test]
    public async Task HandleAsync_TotalCountIsZero_ReturnsZeroTotalPages()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 0));

        var result = await _handler.Handle(new GetNotificationsQuery(userId), CancellationToken.None);

        result.TotalPages.Should().Be(0);
    }

    /// <summary>TotalCount chia hết cho PageSize phải cho ra đúng số trang, không làm tròn lên dư thừa.</summary>
    [Test]
    public async Task HandleAsync_TotalCountExactMultipleOfPageSize_DoesNotRoundUpExtraPage()
    {
        var userId = Guid.NewGuid();
        _repo.GetPagedAsync(userId, null, null, null, null, 1, 10, Arg.Any<CancellationToken>())
            .Returns((new List<Notification>(), 20));

        var result = await _handler.Handle(new GetNotificationsQuery(userId), CancellationToken.None);

        result.TotalPages.Should().Be(2); // 20/10 = 2, không phải 3
    }
}
