using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Application.UseCases.Auth;
using DentalClinic.API.Presentation.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    // MỌI endpoint [AllowAnonymous] dưới đây đều phải mang một policy giới hạn tần suất: không có
    // đăng nhập thì không có gì để đếm ngoài IP, và đây là bề mặt duy nhất kẻ tấn công chạm được
    // mà không cần tài khoản. Endpoint đã [Authorize] thì không cần — token là chốt chặn rồi.
    /// <summary>POST api/auth/login — Bệnh nhân đăng nhập từ app di động (role: Patient)</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var ip = GetClientIp();
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password, AllowedRoles: ["Patient"], IpAddress: ip),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/staff/login — Nhân viên đăng nhập từ web (role: Admin, Dentist, Staff)</summary>
    [HttpPost("staff/login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    public async Task<IActionResult> StaffLogin(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var ip = GetClientIp();
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password, AllowedRoles: ["Admin", "Dentist", "Staff", "Owner"], IpAddress: ip),
            cancellationToken);

        return Ok(result);
    }

    // Tự đăng ký (register / verify-otp / resend-otp) đã bị BỎ. Bất kỳ ai cũng lập được hàng loạt
    // tài khoản rồi giữ kín khung giờ, và giới hạn theo tài khoản không chặn được vì lập tài khoản
    // mới là lách xong. Nay tài khoản bệnh nhân chỉ sinh ra từ POST api/patients/accounts do lễ tân
    // gọi — người thật xác minh người thật. Xem thêm CreatePatientAccountHandler.
    //
    // Đăng nhập Google cũng đã siết tương ứng: chỉ đăng nhập được tài khoản ĐÃ tồn tại, không còn
    // tự tạo tài khoản ở lần đăng nhập đầu (nếu không thì cửa gác này vô nghĩa).

    /// <summary>GET api/auth/me/profile — Lấy thông tin cá nhân của người dùng hiện tại</summary>
    [HttpGet("me/profile")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Không thể xác thực người dùng.");

        var userId = Guid.Parse(userIdString);
        var result = await sender.Send(new GetMyProfileQuery(userId), cancellationToken);
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

        await sender.Send(
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

        await sender.Send(
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
        var result = await sender.Send(
            new CreateAccountCommand(request.FullName, request.Email, request.PhoneNumber, request.Role),
            cancellationToken);

        return CreatedAtAction(nameof(CreateAccount), new { id = result.Id }, result);
    }

    /// <summary>GET api/auth/accounts — Admin lấy danh sách tài khoản</summary>
    [HttpGet("accounts")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAccountsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>PATCH api/auth/accounts/{id}/status — Bật/tắt quyền đăng nhập của 1 tài khoản</summary>
    [HttpPatch("accounts/{id:guid}/status")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> ToggleAccountStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ToggleAccountStatusCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST api/auth/forgot-password — Gửi email đặt lại mật khẩu</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthEmail)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ForgotPasswordCommand(request.Email),
            cancellationToken);

        return Ok(new { message = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu." });
    }

    /// <summary>POST api/auth/reset-password — Đặt lại mật khẩu bằng token</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthOtp)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ResetPasswordCommand(request.Email, request.Token, request.NewPassword),
            cancellationToken);

        return Ok(new { message = "Mật khẩu đã được đặt lại thành công." });
    }

    /// <summary>POST api/auth/google-login — Đăng nhập/đăng ký bằng Google (bệnh nhân, mobile app)</summary>
    [HttpPost("google-login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthLogin)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GoogleLoginCommand(request.IdToken),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>POST api/auth/patient/forgot-password — Gửi mã OTP quên mật khẩu về email (bệnh nhân)</summary>
    [HttpPost("patient/forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthEmail)]
    public async Task<IActionResult> ForgotPasswordOtp(
        [FromBody] ForgotPasswordOtpRequestDto request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ForgotPasswordOtpCommand(request.Email),
            cancellationToken);

        return Ok(new { message = "Mã OTP đã được gửi đến email của bạn." });
    }

    /// <summary>POST api/auth/patient/verify-reset-otp — Xác thực OTP quên mật khẩu, cấp reset token</summary>
    [HttpPost("patient/verify-reset-otp")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.AuthOtp)]
    public async Task<IActionResult> VerifyResetOtp(
        [FromBody] VerifyResetOtpRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
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
