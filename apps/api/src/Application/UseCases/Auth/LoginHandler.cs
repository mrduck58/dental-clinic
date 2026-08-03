using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record LoginCommand(string Email, string Password, string[]? AllowedRoles = null, string? IpAddress = null) : IRequest<LoginResponseDto>;

public class LoginHandler(IUserRepository userRepository, IJwtService jwtService, IActivityLogService activityLogService) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    public async Task<LoginResponseDto> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, ct);

        if (user is null || user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
        {
            await activityLogService.LogAsync(
                userId: null,
                userName: command.Email,
                userRole: "Unknown",
                action: ActivityAction.Login,
                module: ActivityModule.Account,
                description: $"Đăng nhập thất bại với email: {command.Email}",
                status: ActivityStatus.Failed,
                ipAddress: command.IpAddress,
                ct: ct);

            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            var msg = user.Role == "Patient"
                ? "Tài khoản chưa được xác thực. Vui lòng kiểm tra email để nhận mã OTP."
                : "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.";

            await activityLogService.LogAsync(
                userId: user.Id,
                userName: !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email,
                userRole: user.Role,
                action: ActivityAction.Login,
                module: ActivityModule.Account,
                description: $"Đăng nhập thất bại — tài khoản bị vô hiệu hóa: {command.Email}",
                status: ActivityStatus.Failed,
                ipAddress: command.IpAddress,
                ct: ct);

            throw new UnauthorizedAccessException(msg);
        }

        if (command.AllowedRoles is { Length: > 0 } && !command.AllowedRoles.Contains(user.Role))
        {
            await activityLogService.LogAsync(
                userId: user.Id,
                userName: !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email,
                userRole: user.Role,
                action: ActivityAction.Login,
                module: ActivityModule.Account,
                description: $"Đăng nhập thất bại — không đúng cổng đăng nhập: {command.Email} (role: {user.Role})",
                status: ActivityStatus.Failed,
                ipAddress: command.IpAddress,
                ct: ct);

            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        var token = jwtService.GenerateToken(user);

        await activityLogService.LogAsync(
            userId: user.Id,
            userName: !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email,
            userRole: user.Role,
            action: ActivityAction.Login,
            module: ActivityModule.Account,
            description: $"Đăng nhập thành công",
            status: ActivityStatus.Success,
            ipAddress: command.IpAddress,
            ct: ct);

        var profilePic = user.Role switch
        {
            "Patient" => user.Patient?.ProfilePictureUrl,
            "Dentist" => user.Dentist?.ProfilePictureUrl,
            "Staff" => user.Staff?.ProfilePictureUrl,
            _ => null
        };

        return new LoginResponseDto(
            AccessToken: token,
            ExpiresIn: 15 * 60,
            User: new AuthUserDto(user.Id, user.Username ?? user.Email, user.FullName, user.Email, user.Role, user.IsActive, profilePic));
    }
}
