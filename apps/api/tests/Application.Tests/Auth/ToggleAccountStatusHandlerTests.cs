using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class ToggleAccountStatusHandlerTests
{
    private IUserRepository _userRepo = null!;
    private ToggleAccountStatusHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _handler = new ToggleAccountStatusHandler(_userRepo);
    }

    /// <summary>Không tìm thấy tài khoản phải ném NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new ToggleAccountStatusCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Tài khoản đang active phải bị tắt (IsActive = false) và lưu lại.</summary>
    [Test]
    public async Task HandleAsync_ActiveAccount_DeactivatesAndReturnsUpdatedDto()
    {
        var user = User.Create("staff1", "staff1@test.com", "hash", UserRole.Staff, fullName: "Nguyễn Văn A");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new ToggleAccountStatusCommand(user.Id), CancellationToken.None);

        user.IsActive.Should().BeFalse();
        result.IsActive.Should().BeFalse();
        result.Id.Should().Be(user.Id);
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>Tài khoản đang bị tắt phải được bật lại (IsActive = true).</summary>
    [Test]
    public async Task HandleAsync_InactiveAccount_ReactivatesAccount()
    {
        var user = User.Create("staff2", "staff2@test.com", "hash", UserRole.Staff, fullName: "Trần Thị B");
        user.SetActive(false);
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new ToggleAccountStatusCommand(user.Id), CancellationToken.None);

        result.IsActive.Should().BeTrue();
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }
}
