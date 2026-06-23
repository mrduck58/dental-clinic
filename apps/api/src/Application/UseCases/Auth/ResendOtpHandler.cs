using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ResendOtpCommand(string Email);

public class ResendOtpHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IEmailService emailService)
{
    public async Task HandleAsync(ResendOtpCommand command, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, ct)
            ?? throw new NotFoundException("Email không tồn tại trong hệ thống.");

        if (user.IsActive)
            throw new ConflictException("Tài khoản đã được xác thực.");

        if (user.Role != "Patient")
            throw new UnauthorizedAccessException("Không thể gửi OTP cho tài khoản này.");

        await otpRepository.InvalidateAllAsync(command.Email, ct);

        var otp = OtpCode.Create(command.Email);
        await otpRepository.AddAsync(otp, ct);
        await emailService.SendOtpAsync(command.Email, otp.Code, ct);
    }
}
