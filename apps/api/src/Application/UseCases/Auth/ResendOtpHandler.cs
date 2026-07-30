using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ResendOtpCommand(string Email) : IRequest;

public class ResendOtpHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IEmailService emailService) : IRequestHandler<ResendOtpCommand>
{
    public async Task Handle(ResendOtpCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, ct)
            ?? throw new NotFoundException("Email không tồn tại trong hệ thống.");

        if (user.IsActive)
            throw new ConflictException("Tài khoản đã được xác thực.");

        if (user.Role != "Patient")
            throw new UnauthorizedAccessException("Không thể gửi OTP cho tài khoản này.");

        await otpRepository.InvalidateAllAsync(command.Email, OtpPurpose.Registration, ct);

        var otp = OtpCode.Create(command.Email, OtpPurpose.Registration);
        await otpRepository.AddAsync(otp, ct);
        await emailService.SendOtpAsync(command.Email, otp.Code, ct);
    }
}
