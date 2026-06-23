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
public class CreateStaffAccountHandlerTests
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
    /// Cấp tài khoản cho nhân viên chưa có account phải gọi UpdateAsync và gửi email thông tin đăng nhập.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmployeeWithoutAccount_UpdatesAndSendsEmail()
    {
        var user = User.CreateEmployee("emp@test.com", "Staff");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        await handler.HandleAsync(user.Id);

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendStaffCredentialsAsync(
            user.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Nhân viên không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Nhân viên đã có tài khoản rồi không được cấp lại, phải ném ConflictException.
    /// </summary>
    [Test]
    public async Task HandleAsync_AlreadyHasAccount_ThrowsConflictException()
    {
        var user = User.Create("user1", "emp@test.com", "hash", "Staff");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        Func<Task> act = () => handler.HandleAsync(user.Id);

        await act.Should().ThrowAsync<ConflictException>().WithMessage("*đã có tài khoản*");
    }

    /// <summary>
    /// Sau khi cấp tài khoản, nhân viên phải có HasAccount = true trong kết quả trả về.
    /// </summary>
    [Test]
    public async Task HandleAsync_Success_ReturnedDtoHasAccount()
    {
        var user = User.CreateEmployee("emp@test.com", "Staff");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var handler = new CreateStaffAccountHandler(_userRepo, _emailService);

        var result = await handler.HandleAsync(user.Id);

        result.HasAccount.Should().BeTrue();
    }
}
