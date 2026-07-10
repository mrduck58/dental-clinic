using System.Security.Cryptography;
using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Auth;

public record VerifyPasswordResetOtpCommand(string Email, string Code);

/// <summary>Xác thực OTP quên mật khẩu; nếu đúng, cấp một reset token ngắn hạn để dùng ở bước đặt mật khẩu mới (tái sử dụng /api/auth/reset-password).</summary>
public class VerifyPasswordResetOtpHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository)
{
    public async Task<VerifyResetOtpResponseDto> HandleAsync(VerifyPasswordResetOtpCommand command, CancellationToken ct = default)
    {
        var email = command.Email.Trim().ToLower();

        var otp = await otpRepository.GetLatestValidAsync(email, OtpPurpose.PasswordReset, ct)
            ?? throw new UnauthorizedAccessException("Mã OTP không đúng hoặc đã hết hạn.");

        if (otp.Code != command.Code)
            throw new UnauthorizedAccessException("Mã OTP không đúng hoặc đã hết hạn.");

        otp.MarkUsed();
        await otpRepository.UpdateAsync(otp, ct);

        var user = await userRepository.GetByEmailAsync(email, ct)
            ?? throw new UnauthorizedAccessException("Mã OTP không đúng hoặc đã hết hạn.");

        var token = GenerateSecureToken();
        user.SetPasswordResetToken(token, DateTimeOffset.UtcNow.AddMinutes(10));
        await userRepository.UpdateAsync(user, ct);

        return new VerifyResetOtpResponseDto(token);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
