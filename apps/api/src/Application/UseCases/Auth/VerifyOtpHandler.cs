using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record VerifyOtpCommand(string Email, string Code) : IRequest<VerifyOtpResponseDto>;

public class VerifyOtpHandler(
    IUserRepository userRepository,
    IOtpRepository otpRepository,
    IJwtService jwtService) : IRequestHandler<VerifyOtpCommand, VerifyOtpResponseDto>
{
    public async Task<VerifyOtpResponseDto> Handle(VerifyOtpCommand command, CancellationToken ct)
    {
        var otp = await otpRepository.GetLatestValidAsync(command.Email, OtpPurpose.Registration, ct)
            ?? throw new UnauthorizedAccessException("Mã OTP không hợp lệ hoặc đã hết hạn.");

        if (otp.Code != command.Code)
            throw new UnauthorizedAccessException("Mã OTP không đúng.");

        otp.MarkUsed();
        await otpRepository.UpdateAsync(otp, ct);

        var user = await userRepository.GetByEmailAsync(command.Email, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        user.SetActive(true);
        await userRepository.UpdateAsync(user, ct);

        var token = jwtService.GenerateToken(user);

        return new VerifyOtpResponseDto(user.Id, user.Email, user.Role.ToString(), token, 8 * 60 * 60);
    }
}
