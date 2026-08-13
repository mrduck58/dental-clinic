using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class ResetPasswordHandlerTests
{
    private IUserRepository _userRepo = null!;
    private ResetPasswordHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo = Substitute.For<IUserRepository>();
        _handler = new ResetPasswordHandler(_userRepo);
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    /// <summary>
    /// Token hợp lệ và chưa hết hạn phải cập nhật mật khẩu thành công
    /// và gọi UpdateAsync đúng 1 lần để lưu mật khẩu mới xuống DB.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidToken_UpdatesPasswordAndCallsUpdateAsync()
    {
        var user = CreateUserWithResetToken("valid-token", DateTimeOffset.UtcNow.AddHours(1));
        _userRepo.GetByEmailAsync("staff@test.com", Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new ResetPasswordCommand("staff@test.com", "valid-token", "NewPass@123"), CancellationToken.None);

        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi đặt lại mật khẩu thành công, token trên user phải bị xóa
    /// để token không thể dùng lại lần thứ hai (one-time use).
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidToken_ClearsResetTokenAfterReset()
    {
        var user = CreateUserWithResetToken("valid-token", DateTimeOffset.UtcNow.AddHours(1));
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new ResetPasswordCommand("staff@test.com", "valid-token", "NewPass@123"), CancellationToken.None);

        user.PasswordResetToken.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
    }

    /// <summary>
    /// Mật khẩu mới phải được BCrypt hash trước khi lưu,
    /// không bao giờ được lưu plaintext vào DB.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidToken_StoresHashedPassword()
    {
        const string newPassword = "NewPass@123";
        var user = CreateUserWithResetToken("valid-token", DateTimeOffset.UtcNow.AddHours(1));
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new ResetPasswordCommand("staff@test.com", "valid-token", newPassword), CancellationToken.None);

        user.PasswordHash.Should().NotBe(newPassword);
        BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash).Should().BeTrue();
    }

    // ── Error paths ───────────────────────────────────────────────────────────

    /// <summary>
    /// Email không tồn tại trong DB phải ném UnauthorizedAccessException
    /// với message mô tả link không hợp lệ — không tiết lộ lý do chi tiết.
    /// </summary>
    [Test]
    public async Task HandleAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        Func<Task> act = () => _handler.Handle(
            new ResetPasswordCommand("notfound@test.com", "some-token", "NewPass@123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*không hợp lệ*");
    }

    /// <summary>
    /// Token trên request không khớp với token được lưu trên user phải ném exception,
    /// ngăn kẻ tấn công đoán token hoặc dùng token cũ đã bị thay thế.
    /// </summary>
    [Test]
    public async Task HandleAsync_WrongToken_ThrowsUnauthorizedAccessException()
    {
        var user = CreateUserWithResetToken("correct-token", DateTimeOffset.UtcNow.AddHours(1));
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(
            new ResetPasswordCommand("staff@test.com", "wrong-token", "NewPass@123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*không hợp lệ*");
    }

    /// <summary>
    /// Token hết hạn (expiry đã qua) phải bị từ chối dù token string đúng,
    /// đảm bảo link reset chỉ có hiệu lực trong 1 giờ.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExpiredToken_ThrowsUnauthorizedAccessException()
    {
        var user = CreateUserWithResetToken("valid-token", DateTimeOffset.UtcNow.AddHours(-1));
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(
            new ResetPasswordCommand("staff@test.com", "valid-token", "NewPass@123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*hết hạn*");
    }

    /// <summary>
    /// User không có token nào (PasswordResetToken = null) phải ném exception,
    /// xử lý trường hợp user chưa từng yêu cầu forgot-password.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoTokenOnUser_ThrowsUnauthorizedAccessException()
    {
        var user = User.Create("user1", "staff@test.com", BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Staff);
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        Func<Task> act = () => _handler.Handle(
            new ResetPasswordCommand("staff@test.com", "any-token", "NewPass@123"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>
    /// Khi token không hợp lệ, UpdateAsync không được gọi —
    /// đảm bảo mật khẩu không bị thay đổi khi xác thực thất bại.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidToken_DoesNotCallUpdateAsync()
    {
        var user = CreateUserWithResetToken("correct-token", DateTimeOffset.UtcNow.AddHours(1));
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        Assert.CatchAsync(() => _handler.Handle(
            new ResetPasswordCommand("staff@test.com", "wrong-token", "NewPass@123"), CancellationToken.None));

        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Email được normalize (trim + lowercase) trước khi tra cứu,
    /// đảm bảo "  Staff@Test.COM  " tìm đúng user.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmailWithWhitespaceAndUpperCase_NormalizesBeforeLookup()
    {
        var user = CreateUserWithResetToken("valid-token", DateTimeOffset.UtcNow.AddHours(1));
        _userRepo.GetByEmailAsync("staff@test.com", Arg.Any<CancellationToken>()).Returns(user);

        await _handler.Handle(new ResetPasswordCommand("  Staff@Test.COM  ", "valid-token", "NewPass@123"), CancellationToken.None);

        await _userRepo.Received(1).GetByEmailAsync("staff@test.com", Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User CreateUserWithResetToken(string token, DateTimeOffset expiry)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("oldpass");
        var user = User.Create("user1", "staff@test.com", hash, UserRole.Staff);
        user.SetPasswordResetToken(token, expiry);
        return user;
    }
}
