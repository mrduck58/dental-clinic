using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class ChangePasswordHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IActivityLogService _activityLogService = null!;
    private ICurrentUserService _currentUser = null!;
    private ChangePasswordHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _activityLogService = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _handler = new ChangePasswordHandler(_userRepo, _activityLogService, _currentUser);
    }

    private static User CreateUserWithPassword(string password)
        => User.Create("user1", "user1@test.com", BCrypt.Net.BCrypt.HashPassword(password), "Staff");

    /// <summary>Không tìm thấy tài khoản phải ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new ChangePasswordCommand(Guid.NewGuid(), "old", "new"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Mật khẩu hiện tại nhập sai phải ném UnauthorizedAccessException, không đổi mật khẩu.</summary>
    [Test]
    public async Task HandleAsync_WrongCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        var user = CreateUserWithPassword("correct-password");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(new ChangePasswordCommand(user.Id, "wrong-password", "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Đổi mật khẩu hợp lệ phải hash mật khẩu mới, lưu lại và ghi log hoạt động.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_UpdatesPasswordAndLogsActivity()
    {
        var user = CreateUserWithPassword("old-password");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new ChangePasswordCommand(user.Id, "old-password", "new-password"), CancellationToken.None);

        BCrypt.Net.BCrypt.Verify("new-password", user.PasswordHash).Should().BeTrue();
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _activityLogService.Received(1).LogAsync(
            Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Tài khoản chưa từng có mật khẩu (PasswordHash null, ví dụ đăng nhập qua Google) phải ném UnauthorizedAccessException, không đổi mật khẩu.</summary>
    [Test]
    public async Task HandleAsync_NullPasswordHash_ThrowsUnauthorizedAccessException()
    {
        var user = User.CreateGoogleUser("google-user@test.com", "Google User", null);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(new ChangePasswordCommand(user.Id, "any-password", "new-password"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Đổi mật khẩu hợp lệ phải ghi log hoạt động đúng action, module, mô tả, trạng thái và targetId theo thông tin người dùng hiện tại.</summary>
    [Test]
    public async Task HandleAsync_ValidRequest_LogsActivityWithExpectedDetails()
    {
        var user = CreateUserWithPassword("old-password");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var currentUserId = Guid.NewGuid();
        _currentUser.UserId.Returns(currentUserId);
        _currentUser.UserName.Returns("admin1");
        _currentUser.UserRole.Returns("Admin");
        _currentUser.IpAddress.Returns("127.0.0.1");

        await _handler.Handle(new ChangePasswordCommand(user.Id, "old-password", "new-password"), CancellationToken.None);

        await _activityLogService.Received(1).LogAsync(
            currentUserId,
            "admin1",
            "Admin",
            ActivityAction.Edit,
            ActivityModule.Account,
            "Đổi mật khẩu tài khoản",
            ActivityStatus.Success,
            "127.0.0.1",
            user.Id.ToString(),
            Arg.Any<CancellationToken>());
    }
}
