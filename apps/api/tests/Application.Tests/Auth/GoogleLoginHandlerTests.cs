using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Auth;

[TestFixture]
public class GoogleLoginHandlerTests
{
    private IGoogleAuthService _googleAuthService = null!;
    private IUserRepository _userRepo = null!;
    private IJwtService _jwtService = null!;
    private GoogleLoginHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _googleAuthService = Substitute.For<IGoogleAuthService>();
        _userRepo = Substitute.For<IUserRepository>();
        _jwtService = Substitute.For<IJwtService>();
        _jwtService.GenerateToken(Arg.Any<User>()).Returns("fake-jwt-token");
        _handler = new GoogleLoginHandler(_googleAuthService, _userRepo, _jwtService);
    }

    /// <summary>Email Google chưa có tài khoản phải tự tạo user mới với role Patient và báo IsNewUser = true.</summary>
    [Test]
    public async Task HandleAsync_EmailNotRegistered_CreatesNewPatientUser()
    {
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("new@gmail.com", "Người Dùng Mới", "https://pic.url"));
        _userRepo.GetByEmailAsync("new@gmail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(new GoogleLoginCommand("valid-id-token"));

        result.IsNewUser.Should().BeTrue();
        result.User.Role.Should().Be("Patient");
        await _userRepo.Received(1).AddAsync(Arg.Is<User>(u => u.Email == "new@gmail.com"), Arg.Any<CancellationToken>());
    }

    /// <summary>Email Google đã có tài khoản Patient active phải đăng nhập bình thường, IsNewUser = false.</summary>
    [Test]
    public async Task HandleAsync_ExistingActivePatient_LogsInWithoutCreatingUser()
    {
        var existing = User.CreateGoogleUser("existing@gmail.com", "Người Dùng Cũ", null);
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("existing@gmail.com", "Người Dùng Cũ", null));
        _userRepo.GetByEmailAsync("existing@gmail.com", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.HandleAsync(new GoogleLoginCommand("valid-id-token"));

        result.IsNewUser.Should().BeFalse();
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Email đã tồn tại nhưng gắn với tài khoản nhân viên (không phải Patient) phải bị chặn.</summary>
    [Test]
    public async Task HandleAsync_ExistingNonPatientAccount_ThrowsUnauthorizedAccessException()
    {
        var staff = User.Create("staff1", "staff@gmail.com", BCrypt.Net.BCrypt.HashPassword("pass"), "Staff");
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("staff@gmail.com", "Nhân Viên", null));
        _userRepo.GetByEmailAsync("staff@gmail.com", Arg.Any<CancellationToken>()).Returns(staff);

        Func<Task> act = () => _handler.HandleAsync(new GoogleLoginCommand("valid-id-token"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>Tài khoản Patient đã bị vô hiệu hóa phải bị chặn đăng nhập.</summary>
    [Test]
    public async Task HandleAsync_ExistingInactivePatient_ThrowsUnauthorizedAccessException()
    {
        var inactive = User.CreateGoogleUser("inactive@gmail.com", "Bị Khóa", null);
        inactive.SetActive(false);
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("inactive@gmail.com", "Bị Khóa", null));
        _userRepo.GetByEmailAsync("inactive@gmail.com", Arg.Any<CancellationToken>()).Returns(inactive);

        Func<Task> act = () => _handler.HandleAsync(new GoogleLoginCommand("valid-id-token"));

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    /// <summary>Đăng nhập Google với email chưa đăng ký phải trả về đầy đủ thông tin: access token do JwtService sinh ra, thời hạn 15 phút, và Username fallback về Email vì user Google mới tạo chưa có username.</summary>
    [Test]
    public async Task HandleAsync_EmailNotRegistered_ReturnsFullResponseShape()
    {
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("new@gmail.com", "Người Dùng Mới", "https://pic.url"));
        _userRepo.GetByEmailAsync("new@gmail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.HandleAsync(new GoogleLoginCommand("valid-id-token"));

        result.AccessToken.Should().Be("fake-jwt-token");
        result.ExpiresIn.Should().Be(15 * 60);
        result.User.Username.Should().Be("new@gmail.com");
        result.User.FullName.Should().Be("Người Dùng Mới");
        result.User.Email.Should().Be("new@gmail.com");
        result.User.IsActive.Should().BeTrue();
        result.User.ProfilePictureUrl.Should().Be("https://pic.url");
        _jwtService.Received(1).GenerateToken(Arg.Is<User>(u => u.Email == "new@gmail.com"));
    }

    /// <summary>Tài khoản Patient đã tồn tại và có sẵn Username phải trả về đúng Username đó, không fallback về Email.</summary>
    [Test]
    public async Task HandleAsync_ExistingPatientWithUsername_ReturnsActualUsername()
    {
        var existing = User.Create("patient_username", "existing2@gmail.com", BCrypt.Net.BCrypt.HashPassword("pass"), "Patient");
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("existing2@gmail.com", "Người Dùng Cũ", null));
        _userRepo.GetByEmailAsync("existing2@gmail.com", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.HandleAsync(new GoogleLoginCommand("valid-id-token"));

        result.User.Username.Should().Be("patient_username");
        _jwtService.Received(1).GenerateToken(existing);
    }
}
