using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    LoginHandler loginHandler,
    RegisterHandler registerHandler,
    VerifyOtpHandler verifyOtpHandler,
    ResendOtpHandler resendOtpHandler,
    FillProfileHandler fillProfileHandler,
    GetMyProfileHandler getMyProfileHandler,
    ChangePasswordHandler changePasswordHandler,
    CreateAccountHandler createAccountHandler,
    GetAccountsHandler getAccountsHandler,
    ToggleAccountStatusHandler toggleAccountStatusHandler,
    ForgotPasswordHandler forgotPasswordHandler,
    ResetPasswordHandler resetPasswordHandler,
    GoogleLoginHandler googleLoginHandler,
    ForgotPasswordOtpHandler forgotPasswordOtpHandler,
    VerifyPasswordResetOtpHandler verifyPasswordResetOtpHandler) : ControllerBase
{
    /// <summary>POST api/auth/login — Bệnh nhân đăng nhập từ app di động (role: Patient)</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var ip = GetClientIp();
        var result = await loginHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password, AllowedRoles: ["Patient"], IpAddress: ip),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/staff/login — Nhân viên đăng nhập từ web (role: Admin, Dentist, Staff)</summary>
    [HttpPost("staff/login")]
    [AllowAnonymous]
    public async Task<IActionResult> StaffLogin(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var ip = GetClientIp();
        var result = await loginHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password, AllowedRoles: ["Admin", "Dentist", "Staff", "Owner"], IpAddress: ip),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/register — Bệnh nhân tự đăng ký, gửi OTP về email</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await registerHandler.HandleAsync(
            new RegisterCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/verify-otp — Xác thực mã OTP, kích hoạt tài khoản và cấp JWT</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp(
        [FromBody] VerifyOtpRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await verifyOtpHandler.HandleAsync(
            new VerifyOtpCommand(request.Email, request.Code),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/resend-otp — Gửi lại mã OTP</summary>
    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendOtpRequestDto request,
        CancellationToken cancellationToken)
    {
        await resendOtpHandler.HandleAsync(
            new ResendOtpCommand(request.Email),
            cancellationToken);

        return Ok(new { message = "Mã OTP mới đã được gửi đến email của bạn." });
    }

    /// <summary>GET api/auth/me/profile — Lấy thông tin cá nhân của người dùng hiện tại</summary>
    [HttpGet("me/profile")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không thể xác thực người dùng.");

        var userId = Guid.Parse(userIdString);
        var result = await getMyProfileHandler.HandleAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT api/auth/me/profile — Điền và cập nhật thông tin cá nhân</summary>
    [HttpPut("me/profile")]
    [Authorize]
    public async Task<IActionResult> FillProfile(
        [FromBody] FillProfileRequestDto request,
        CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không thể xác thực người dùng.");

        var userId = Guid.Parse(userIdString);

        await fillProfileHandler.HandleAsync(
            new FillProfileCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.FullName,
                request.PhoneNumber,
                request.DateOfBirth,
                request.Gender,
                request.Address,
                request.ProfilePictureUrl,
                request.Bio,
                request.Education,
                request.Specialty,
                request.YearsOfExperience),
            cancellationToken);

        return Ok(new { message = "Đã cập nhật thông tin cá nhân." });
    }

    /// <summary>PUT api/auth/me/change-password — Đổi mật khẩu tài khoản hiện tại</summary>
    [HttpPut("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không thể xác thực người dùng.");

        var userId = Guid.Parse(userIdString);

        await changePasswordHandler.HandleAsync(
            new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return Ok(new { message = "Đã đổi mật khẩu thành công." });
    }

    /// <summary>POST api/auth/logout — Đăng xuất (client xóa token; server-side hook cho tương lai)</summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Đăng xuất thành công." });
    }

    /// <summary>POST api/auth/accounts — Admin tạo tài khoản cho nhân viên</summary>
    [HttpPost("accounts")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> CreateAccount(
        [FromBody] CreateAccountRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await createAccountHandler.HandleAsync(
            new CreateAccountCommand(request.FullName, request.Email, request.PhoneNumber, request.Role),
            cancellationToken);

        return CreatedAtAction(nameof(CreateAccount), new { id = result.Id }, result);
    }

    /// <summary>GET api/auth/accounts — Admin lấy danh sách tài khoản</summary>
    [HttpGet("accounts")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var result = await getAccountsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>PATCH api/auth/accounts/{id}/status — Bật/tắt quyền đăng nhập của tài khoản</summary>
    [HttpPatch("accounts/{id:guid}/status")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> ToggleAccountStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await toggleAccountStatusHandler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/auth/forgot-password — Gửi email đặt lại mật khẩu</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await forgotPasswordHandler.HandleAsync(
            new ForgotPasswordCommand(request.Email),
            cancellationToken);

        return Ok(new { message = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu." });
    }

    /// <summary>POST api/auth/reset-password — Đặt lại mật khẩu bằng token</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await resetPasswordHandler.HandleAsync(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            cancellationToken);

        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }

    /// <summary>POST api/auth/google-login — Đăng nhập/đăng ký bằng Google (bệnh nhân, mobile app)</summary>
    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await googleLoginHandler.HandleAsync(
            new GoogleLoginCommand(request.IdToken),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/patient/forgot-password — Gửi mã OTP quên mật khẩu về email (bệnh nhân)</summary>
    [HttpPost("patient/forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPasswordOtp(
        [FromBody] ForgotPasswordOtpRequestDto request,
        CancellationToken cancellationToken)
    {
        await forgotPasswordOtpHandler.HandleAsync(
            new ForgotPasswordOtpCommand(request.Email),
            cancellationToken);

        return Ok(new { message = "Mã OTP đã được gửi đến email của bạn." });
    }

    /// <summary>POST api/auth/patient/verify-reset-otp — Xác thực OTP quên mật khẩu, cấp reset token</summary>
    [HttpPost("patient/verify-reset-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyResetOtp(
        [FromBody] VerifyResetOtpRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await verifyPasswordResetOtpHandler.HandleAsync(
            new VerifyPasswordResetOtpCommand(request.Email, request.Code),
            cancellationToken);

        return Ok(result);
    }

    private string? GetClientIp()
    {
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
