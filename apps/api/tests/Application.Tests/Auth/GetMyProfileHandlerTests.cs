using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class GetMyProfileHandlerTests
{
    private IUserRepository _userRepo = null!;
    private GetMyProfileHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _handler = new GetMyProfileHandler(_userRepo);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tài khoản tồn tại phải trả về UserProfileDto với email khớp đúng,
    /// để client hiển thị thông tin cá nhân người dùng đang đăng nhập.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_ReturnsCorrectEmail()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient1", "patient@test.com", "hash", "Patient", "0901234567");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(userId);

        result.Email.Should().Be("patient@test.com");
    }

    /// <summary>
    /// Số điện thoại được truyền vào User.Create phải xuất hiện đúng trong DTO trả về,
    /// để bệnh nhân thấy thông tin liên lạc của mình trên màn hình hồ sơ.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingUser_ReturnsCorrectPhoneNumber()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient2", "patient2@test.com", "hash", "Patient", "0912345678");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(userId);

        result.PhoneNumber.Should().Be("0912345678");
    }

    /// <summary>
    /// Tài khoản không có số điện thoại phải trả về PhoneNumber = null,
    /// không được trả về chuỗi rỗng vì client dùng null-check để ẩn trường này.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserWithNoPhone_ReturnsNullPhoneNumber()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient3", "patient3@test.com", "hash", "Patient");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(userId);

        result.PhoneNumber.Should().BeNull();
    }

    /// <summary>
    /// Tài khoản mới chưa điền hồ sơ phải trả về các trường optional đều null,
    /// tránh hiển thị giá trị rác trên màn hình hồ sơ bệnh nhân.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewUser_ReturnsNullOptionalFields()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("patient4", "patient4@test.com", "hash", "Patient");
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.HandleAsync(userId);

        result.DateOfBirth.Should().BeNull();
        result.Gender.Should().BeNull();
        result.ProfilePictureUrl.Should().BeNull();
    }

    // ── Error path ────────────────────────────────────────────────────────────

    /// <summary>
    /// userId không tồn tại trong hệ thống phải ném NotFoundException,
    /// để controller trả về 404 thay vì 500 khi token hợp lệ nhưng user đã bị xóa.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.HandleAsync(userId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Khi userId không tồn tại, handler không được cố tiếp tục xử lý
    /// mà phải dừng ngay tại bước tìm kiếm để tránh NullReferenceException downstream.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_DoesNotReturnResult()
    {
        var userId = Guid.NewGuid();
        _userRepo.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await _handler.HandleAsync(userId);

        await act.Should().ThrowAsync<NotFoundException>();
        // Assertion: kiểm tra ném exception, không trả về dto
    }
}
