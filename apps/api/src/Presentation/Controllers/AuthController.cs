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
    CreateAccountHandler createAccountHandler,
    GetAccountsHandler getAccountsHandler) : ControllerBase
{
    /// <summary>POST api/auth/login — Bệnh nhân đăng nhập từ app di động (role: Patient)</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await loginHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password, AllowedRoles: ["Patient"]),
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
        var result = await loginHandler.HandleAsync(
            new LoginCommand(request.Email, request.Password, AllowedRoles: ["Admin", "Dentist", "Staff"]),
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

    /// <summary>PUT api/auth/me/profile — Điền thông tin cá nhân sau đăng ký</summary>
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
            new FillProfileCommand(userId, request.FirstName, request.LastName,
                request.PhoneNumber, request.DateOfBirth, request.Gender),
            cancellationToken);

        return NoContent();
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var result = await getAccountsHandler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
