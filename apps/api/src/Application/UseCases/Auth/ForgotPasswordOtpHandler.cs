using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ForgotPasswordOtpCommand(string Email) : IRequest;

/// <summary>Bắt đầu luồng quên mật khẩu bằng OTP cho bệnh nhân (mobile app).</summary>
public class ForgotPasswordOtpHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IEmailService emailService) : IRequestHandler<ForgotPasswordOtpCommand>
{
    public async Task Handle(ForgotPasswordOtpCommand command, CancellationToken ct)
    {
        var email = command.Email.Trim().ToLower();
        var user = await userRepository.GetByEmailAsync(email, ct);

        if (user is null || user.Role != "Patient" || !user.IsActive)
            throw new NotFoundException("Email chưa được đăng ký.");

        await otpRepository.InvalidateAllAsync(email, OtpPurpose.PasswordReset, ct);
        var otp = OtpCode.Create(email, OtpPurpose.PasswordReset);
        await otpRepository.AddAsync(otp, ct);
        await emailService.SendPasswordResetOtpAsync(email, otp.Code, ct);
    }
}
