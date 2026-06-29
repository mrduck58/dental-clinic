using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class FillProfileHandlerTests
{
    private IUserRepository _userRepo = null!;
    private FillProfileHandler _handler = null!;

    private static readonly Guid TestUserId = Guid.NewGuid();

    private static readonly FillProfileCommand ValidCommand = new(
        UserId: TestUserId,
        FirstName: "An",
        LastName: "Nguyễn",
        FullName: null,
        PhoneNumber: "0901234567",
        DateOfBirth: new DateOnly(2000, 1, 15),
        Gender: "Male");

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _handler = new FillProfileHandler(_userRepo);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Cập nhật hồ sơ thành công phải gọi UpdateAsync đúng 1 lần để lưu thay đổi vào database.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_CallsUpdateAsyncOnce()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// FullName phải được ghép theo thứ tự "LastName FirstName" (quy tắc tiếng Việt).
    /// Ví dụ: LastName="Nguyễn", FirstName="An" → FullName="Nguyễn An".
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_SetsFullNameAsLastNameSpaceFirstName()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        user.FullName.Should().Be("Nguyễn An");
    }

    /// <summary>
    /// Số điện thoại trong lệnh phải được cập nhật đúng vào hồ sơ người dùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_SetsPhoneNumberCorrectly()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        user.PhoneNumber.Should().Be(ValidCommand.PhoneNumber);
    }

    /// <summary>
    /// Ngày sinh và giới tính trong lệnh phải được cập nhật đúng vào hồ sơ người dùng.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_SetsDobAndGenderCorrectly()
    {
        var user = CreatePatientUser();
        _userRepo.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>()).Returns(user);

        await _handler.HandleAsync(ValidCommand);

        user.DateOfBirth.Should().Be(ValidCommand.DateOfBirth);
        user.Gender.Should().Be(ValidCommand.Gender);
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    /// <summary>
    /// UserId không tồn tại trong database phải ném NotFoundException.
    /// Trường hợp này xảy ra khi token hợp lệ nhưng user đã bị xóa khỏi hệ thống.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.HandleAsync(ValidCommand);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Khi user không tồn tại, UpdateAsync không được gọi vì không có gì để cập nhật.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_DoesNotCallUpdateAsync()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        Assert.CatchAsync(() => _handler.HandleAsync(ValidCommand));

        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static User CreatePatientUser() =>
        User.Create("patient1", "patient@test.com", "hash", "Patient");
}
