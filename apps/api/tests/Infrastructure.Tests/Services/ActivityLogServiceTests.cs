using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using FluentAssertions;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class ActivityLogServiceTests
{
    private IActivityLogRepository _repo = null!;
    private ILogger<ActivityLogService> _logger = null!;
    private ActivityLogService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repo   = Substitute.For<IActivityLogRepository>();
        _logger = Substitute.For<ILogger<ActivityLogService>>();
        _service = new ActivityLogService(_repo, _logger);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// LogAsync phải gọi repository.AddAsync đúng 1 lần khi mọi thứ bình thường,
    /// đảm bảo mỗi lời gọi log tạo ra đúng 1 record trong database.
    /// </summary>
    [Test]
    public async Task LogAsync_ValidCall_CallsRepositoryAddAsyncOnce()
    {
        await _service.LogAsync(
            userId:      Guid.NewGuid(),
            userName:    "admin@test.com",
            userRole:    "Admin",
            action:      ActivityAction.Create,
            module:      ActivityModule.Account,
            description: "Tạo tài khoản",
            status:      ActivityStatus.Success);

        await _repo.Received(1).AddAsync(Arg.Any<ActivityLog>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// ActivityLog được truyền vào AddAsync phải có đúng các trường từ tham số LogAsync,
    /// tránh tình trạng log ghi sai thông tin người dùng hoặc hành động.
    /// </summary>
    [Test]
    public async Task LogAsync_ValidCall_CreatesLogWithCorrectFields()
    {
        ActivityLog? captured = null;
        await _repo.AddAsync(Arg.Do<ActivityLog>(l => captured = l), Arg.Any<CancellationToken>());

        var userId = Guid.NewGuid();
        await _service.LogAsync(
            userId:      userId,
            userName:    "staff@test.com",
            userRole:    "Staff",
            action:      ActivityAction.Edit,
            module:      ActivityModule.Medicine,
            description: "Sửa thông tin thuốc",
            status:      ActivityStatus.Success,
            ipAddress:   "10.0.0.1",
            targetId:    "med-456");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(userId);
        captured.UserName.Should().Be("staff@test.com");
        captured.Action.Should().Be("edit");
        captured.Module.Should().Be("medicine");
        captured.IpAddress.Should().Be("10.0.0.1");
        captured.TargetId.Should().Be("med-456");
        captured.Status.Should().Be("success");
    }

    /// <summary>
    /// LogAsync không được throw exception khi repository.AddAsync ném lỗi —
    /// lỗi ghi log không được làm hỏng luồng nghiệp vụ chính của ứng dụng.
    /// </summary>
    [Test]
    public async Task LogAsync_RepositoryThrows_DoesNotPropagateException()
    {
        _repo.AddAsync(Arg.Any<ActivityLog>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new InvalidOperationException("DB connection lost"));

        Func<Task> act = () => _service.LogAsync(
            Guid.NewGuid(), "user", "Staff",
            ActivityAction.Delete, ActivityModule.Post,
            "Xóa bài viết", ActivityStatus.Success);

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Khi repository ném lỗi, service phải ghi LogWarning — không được im lặng hoàn toàn
    /// vì kỹ sư cần biết hệ thống đang bỏ sót log để khắc phục kịp thời.
    /// </summary>
    [Test]
    public async Task LogAsync_RepositoryThrows_LogsWarning()
    {
        _repo.AddAsync(Arg.Any<ActivityLog>(), Arg.Any<CancellationToken>())
             .ThrowsAsync(new Exception("timeout"));

        await _service.LogAsync(
            null, "unknown", "Unknown",
            ActivityAction.Login, ActivityModule.Account,
            "Đăng nhập thất bại", ActivityStatus.Failed);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// UserId null phải được chấp nhận (log đăng nhập thất bại chưa biết user),
    /// không được throw NullReferenceException hay lỗi validation.
    /// </summary>
    [Test]
    public async Task LogAsync_NullUserId_DoesNotThrow()
    {
        Func<Task> act = () => _service.LogAsync(
            userId:      null,
            userName:    "unknown@test.com",
            userRole:    "Unknown",
            action:      ActivityAction.Login,
            module:      ActivityModule.Account,
            description: "Đăng nhập thất bại",
            status:      ActivityStatus.Failed,
            ipAddress:   "203.0.113.5");

        await act.Should().NotThrowAsync();
        await _repo.Received(1).AddAsync(Arg.Any<ActivityLog>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// IpAddress và TargetId là optional — truyền null phải hoạt động bình thường.
    /// </summary>
    [Test]
    public async Task LogAsync_NullOptionalFields_CallsRepositorySuccessfully()
    {
        Func<Task> act = () => _service.LogAsync(
            Guid.NewGuid(), "user", "Staff",
            ActivityAction.View, ActivityModule.Service,
            "Xem danh sách dịch vụ", ActivityStatus.Success,
            ipAddress: null, targetId: null);

        await act.Should().NotThrowAsync();
        await _repo.Received(1).AddAsync(Arg.Any<ActivityLog>(), Arg.Any<CancellationToken>());
    }
}
