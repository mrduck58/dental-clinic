using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record RegisterCommand(string Email, string Password) : IRequest<RegisterResponseDto>;

public class RegisterHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IEmailService emailService) : IRequestHandler<RegisterCommand, RegisterResponseDto>
{
    public async Task<RegisterResponseDto> Handle(RegisterCommand command, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new ConflictException("Email này đã được đăng ký. Vui lòng dùng email khác.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(command.Password, workFactor: 12);
        var user = User.Create(command.Email, command.Email, passwordHash, "Patient");
        user.SetActive(false); // Kích hoạt sau khi xác thực OTP

        await userRepository.AddAsync(user, ct);

        // Tạo và gửi OTP
        await otpRepository.InvalidateAllAsync(command.Email, OtpPurpose.Registration, ct);
        var otp = OtpCode.Create(command.Email, OtpPurpose.Registration);
        await otpRepository.AddAsync(otp, ct);
        await emailService.SendOtpAsync(command.Email, otp.Code, ct);

        return new RegisterResponseDto(
            command.Email,
            "Mã xác thực đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.");
    }
}
