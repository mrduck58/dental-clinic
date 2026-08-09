using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
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

    /// <summary>
    /// Google là phương thức ĐĂNG NHẬP, không phải phương thức đăng ký. Trước đây lần đăng nhập đầu
    /// tự tạo luôn tài khoản — không OTP, không xác minh — nên đó là đường lập tài khoản hàng loạt
    /// dễ hơn cả /auth/register và làm vô hiệu việc để lễ tân gác cửa tạo tài khoản.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmailNotRegistered_RejectsAndCreatesNothing()
    {
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("new@gmail.com", "Người Dùng Mới", "https://pic.url"));
        _userRepo.GetByEmailAsync("new@gmail.com", Arg.Any<CancellationToken>()).Returns((User?)null);

        Func<Task> act = () => _handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("*liên hệ phòng khám*");
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Email Google đã có tài khoản Patient active phải đăng nhập bình thường, IsNewUser = false.</summary>
    [Test]
    public async Task HandleAsync_ExistingActivePatient_LogsInWithoutCreatingUser()
    {
        var existing = User.CreateGoogleUser("existing@gmail.com", "Người Dùng Cũ", null);
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("existing@gmail.com", "Người Dùng Cũ", null));
        _userRepo.GetByEmailAsync("existing@gmail.com", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        result.IsNewUser.Should().BeFalse();
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Email đã tồn tại nhưng gắn với tài khoản nhân viên (không phải Patient) phải bị chặn.</summary>
    [Test]
    public async Task HandleAsync_ExistingNonPatientAccount_ThrowsUnauthorizedAccessException()
    {
        var staff = User.Create("staff1", "staff@gmail.com", BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Staff);
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("staff@gmail.com", "Nhân Viên", null));
        _userRepo.GetByEmailAsync("staff@gmail.com", Arg.Any<CancellationToken>()).Returns(staff);

        Func<Task> act = () => _handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

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

        Func<Task> act = () => _handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }


    /// <summary>Tài khoản Patient đã tồn tại và có sẵn Username phải trả về đúng Username đó, không fallback về Email.</summary>
    [Test]
    public async Task HandleAsync_ExistingPatientWithUsername_ReturnsActualUsername()
    {
        var existing = User.Create("patient_username", "existing2@gmail.com", BCrypt.Net.BCrypt.HashPassword("pass"), UserRole.Patient);
        _googleAuthService.VerifyIdTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("existing2@gmail.com", "Người Dùng Cũ", null));
        _userRepo.GetByEmailAsync("existing2@gmail.com", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(new GoogleLoginCommand("valid-id-token"), CancellationToken.None);

        result.User.Username.Should().Be("patient_username");
        _jwtService.Received(1).GenerateToken(existing);
    }
}
