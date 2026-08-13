using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Repositories;

[TestFixture]
public class NotificationRepositoryTests
{
    private AppDbContext _db = null!;
    private NotificationRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _sut = new NotificationRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    /// <summary>AddAsync phải lưu 1 thông báo vào DB.</summary>
    [Test]
    public async Task AddAsync_ValidNotification_PersistsToDatabase()
    {
        var userId = Guid.NewGuid();
        await _sut.AddAsync(MakeNotification(userId));

        (await _db.Notifications.CountAsync()).Should().Be(1);
    }

    /// <summary>AddRangeAsync phải lưu nhiều thông báo cùng lúc.</summary>
    [Test]
    public async Task AddRangeAsync_MultipleNotifications_PersistsAll()
    {
        var userId = Guid.NewGuid();
        await _sut.AddRangeAsync([MakeNotification(userId), MakeNotification(userId)]);

        (await _db.Notifications.CountAsync()).Should().Be(2);
    }

    /// <summary>GetByIdAsync với id tồn tại phải trả về đúng thông báo.</summary>
    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsNotification()
    {
        var notification = MakeNotification(Guid.NewGuid());
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(notification.Id);

        result.Should().NotBeNull();
    }

    /// <summary>GetPagedAsync phải chỉ trả về thông báo của đúng user, không lẫn user khác.</summary>
    [Test]
    public async Task GetPagedAsync_FiltersByUserId()
    {
        var userId = Guid.NewGuid();
        _db.Notifications.AddRange(MakeNotification(userId), MakeNotification(Guid.NewGuid()));
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(userId);

        total.Should().Be(1);
        items[0].UserId.Should().Be(userId);
    }

    /// <summary>GetPagedAsync phải lọc đồng thời theo type/priority/isRead khi được truyền.</summary>
    [Test]
    public async Task GetPagedAsync_FiltersByTypePriorityAndIsRead()
    {
        var userId = Guid.NewGuid();
        var match = MakeNotification(userId, type: NotificationType.Appointment, priority: NotificationPriority.High);
        var wrongType = MakeNotification(userId, type: NotificationType.Invoice, priority: NotificationPriority.High);
        var read = MakeNotification(userId, type: NotificationType.Appointment, priority: NotificationPriority.High);
        read.MarkAsRead();
        _db.Notifications.AddRange(match, wrongType, read);
        await _db.SaveChangesAsync();

        var (items, total) = await _sut.GetPagedAsync(
            userId, type: NotificationType.Appointment, priority: NotificationPriority.High, isRead: false);

        total.Should().Be(1);
        items[0].Id.Should().Be(match.Id);
    }

    /// <summary>GetUnreadCountAsync phải chỉ đếm thông báo chưa đọc của đúng user.</summary>
    [Test]
    public async Task GetUnreadCountAsync_CountsOnlyUnreadForUser()
    {
        var userId = Guid.NewGuid();
        var unread = MakeNotification(userId);
        var read = MakeNotification(userId);
        read.MarkAsRead();
        _db.Notifications.AddRange(unread, read, MakeNotification(Guid.NewGuid()));
        await _db.SaveChangesAsync();

        var count = await _sut.GetUnreadCountAsync(userId);

        count.Should().Be(1);
    }

    /// <summary>GetUnreadByUserAsync phải trả về đúng danh sách thông báo chưa đọc của user đó.</summary>
    [Test]
    public async Task GetUnreadByUserAsync_ReturnsOnlyUnreadForUser()
    {
        var userId = Guid.NewGuid();
        var unread = MakeNotification(userId);
        var read = MakeNotification(userId);
        read.MarkAsRead();
        _db.Notifications.AddRange(unread, read);
        await _db.SaveChangesAsync();

        var result = await _sut.GetUnreadByUserAsync(userId);

        result.Should().ContainSingle(n => n.Id == unread.Id);
    }

    /// <summary>UpdateAsync phải lưu lại trạng thái đã đọc.</summary>
    [Test]
    public async Task UpdateAsync_MarkedAsRead_PersistsChange()
    {
        var notification = MakeNotification(Guid.NewGuid());
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        notification.MarkAsRead();
        await _sut.UpdateAsync(notification);

        var reloaded = await _db.Notifications.FindAsync(notification.Id);
        reloaded!.IsRead.Should().BeTrue();
    }

    /// <summary>UpdateRangeAsync phải lưu lại thay đổi cho tất cả thông báo trong danh sách.</summary>
    [Test]
    public async Task UpdateRangeAsync_MultipleNotifications_PersistsAllChanges()
    {
        var a = MakeNotification(Guid.NewGuid());
        var b = MakeNotification(Guid.NewGuid());
        _db.Notifications.AddRange(a, b);
        await _db.SaveChangesAsync();

        a.MarkAsRead();
        b.MarkAsRead();
        await _sut.UpdateRangeAsync([a, b]);

        (await _db.Notifications.CountAsync(n => n.IsRead)).Should().Be(2);
    }

    /// <summary>DeleteAsync với id tồn tại phải xóa khỏi DB.</summary>
    [Test]
    public async Task DeleteAsync_ExistingId_RemovesFromDatabase()
    {
        var notification = MakeNotification(Guid.NewGuid());
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        await _sut.DeleteAsync(notification.Id);

        (await _db.Notifications.CountAsync()).Should().Be(0);
    }

    /// <summary>DeleteAsync với id không tồn tại phải bỏ qua, không ném lỗi.</summary>
    [Test]
    public async Task DeleteAsync_UnknownId_DoesNotThrow()
    {
        Func<Task> act = () => _sut.DeleteAsync(Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    /// <summary>GetAppointmentReminderKeysAsync phải chỉ lấy thông báo loại Appointment của đúng user.</summary>
    [Test]
    public async Task GetAppointmentReminderKeysAsync_ReturnsOnlyAppointmentTypeForUser()
    {
        var userId = Guid.NewGuid();
        var appt = MakeNotification(userId, type: NotificationType.Appointment, relatedEntityId: "appt-1");
        var other = MakeNotification(userId, type: NotificationType.Invoice, relatedEntityId: "inv-1");
        _db.Notifications.AddRange(appt, other, MakeNotification(Guid.NewGuid(), type: NotificationType.Appointment));
        await _db.SaveChangesAsync();

        var result = await _sut.GetAppointmentReminderKeysAsync(userId);

        result.Should().ContainSingle(k => k.RelatedEntityId == "appt-1");
    }

    private static Notification MakeNotification(
        Guid userId,
        string type = NotificationType.Appointment,
        string priority = NotificationPriority.Medium,
        string? relatedEntityId = null)
        => Notification.Create(userId, type, priority, "Tiêu đề test", "Nội dung test", "Appointment", relatedEntityId);
}
