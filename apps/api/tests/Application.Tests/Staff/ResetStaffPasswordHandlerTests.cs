using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Staff;

[TestFixture]
public class ResetStaffPasswordHandlerTests
{
    private IUserRepository _userRepo = null!;
    private IEmailService _emailService = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _emailService = Substitute.For<IEmailService>();
    }

    /// <summary>
    /// Reset mật khẩu nhân viên tồn tại phải gọi UpdateAsync và gửi email
    /// với mật khẩu tạm thời mới để nhân viên đăng nhập lại.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_UpdatesAndSendsEmail()
    {
        var user = MakeEmployee();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        await handler.HandleAsync(user.Id);

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendStaffCredentialsAsync(
            user.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên không tồn tại phải ném NotFoundException, không gửi email.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Mật khẩu tạm thời trả về phải có độ dài 8 ký tự đúng theo cấu hình.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_ReturnsTemporaryPassword8Chars()
    {
        var user = MakeEmployee();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        var result = await handler.HandleAsync(user.Id);

        result.TemporaryPassword.Should().HaveLength(8);
    }

    /// <summary>
    /// Nhân viên chưa có tài khoản (Username null) và không có FullName phải dùng Email
    /// làm tên hiển thị trong email gửi mật khẩu mới (fallback cuối cùng của chuỗi ??).
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFullNameAndNoUsername_EmailUsesEmailAsDisplayName()
    {
        var user = User.CreateEmployee("emp@test.com", "Staff", fullName: null);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        await handler.HandleAsync(user.Id);

        await _emailService.Received(1).SendStaffCredentialsAsync(
            user.Email, user.Email, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên đã có tài khoản (Username khác null) nhưng không có FullName phải dùng
    /// Username làm tên hiển thị (fallback thứ hai của chuỗi ??).
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFullNameButHasUsername_EmailUsesUsernameAsDisplayName()
    {
        var user = User.Create("myusername", "emp@test.com", "hash", "Staff", fullName: null);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        await handler.HandleAsync(user.Id);

        await _emailService.Received(1).SendStaffCredentialsAsync(
            user.Email, "myusername", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Kết quả trả về phải chứa đúng Id và Email của nhân viên đã reset mật khẩu,
    /// không bị lẫn với các trường khác.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_ReturnsCorrectIdAndEmail()
    {
        var user = MakeEmployee();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new ResetStaffPasswordHandler(_userRepo, _emailService);

        var result = await handler.HandleAsync(user.Id);

        result.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
    }

    private static User MakeEmployee()
        => User.CreateEmployee("emp@test.com", "Staff", "0901234567", "Nhân Viên Test");
}
