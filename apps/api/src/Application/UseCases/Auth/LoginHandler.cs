using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record LoginCommand(string Email, string Password, string[]? AllowedRoles = null, string? IpAddress = null) : IRequest<LoginResponseDto>;

public class LoginHandler(IUserRepository userRepository, IJwtService jwtService, IActivityLogService activityLogService) : IRequestHandler<LoginCommand, LoginResponseDto>
{
    /// <summary>Số lần sai liên tiếp trước khi khóa. Đủ rộng cho người gõ nhầm, đủ chặt để việc dò mật khẩu vô nghĩa.</summary>
    private const int MaxFailedAttempts = 5;

    /// <summary>
    /// Thời gian khóa. Ngắn có chủ đích: giới hạn tần suất theo IP mới là tuyến phòng thủ chính,
    /// còn khóa tài khoản chỉ để chặn kiểu dò rải từ nhiều IP. Khóa càng lâu thì càng dễ bị lợi dụng
    /// để chặn người dùng thật.
    /// </summary>
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResponseDto> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, ct);
        var now = DateTimeOffset.UtcNow;

        // Xét khóa TRƯỚC khi kiểm tra mật khẩu — nếu xét sau, mỗi request vẫn tốn một lần băm BCrypt
        // (workFactor 12, cố tình chậm), biến chính cơ chế bảo vệ thành đòn bẩy để làm nghẽn server.
        if (user is not null && user.IsLockedOut(now))
        {
            await LogFailureAsync(user, command, "tài khoản đang bị tạm khóa do đăng nhập sai nhiều lần", ct);

            var minutesLeft = Math.Max(1, (int)Math.Ceiling((user.LockoutEndAt!.Value - now).TotalMinutes));
            throw new UnauthorizedAccessException(
                $"Tài khoản đang tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau {minutesLeft} phút.");
        }

        if (user is null || user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
        {
            // Chỉ đếm được khi tài khoản có thật. Email không tồn tại thì không có gì để khóa —
            // đó chính là phần mà giới hạn tần suất theo IP đảm nhiệm.
            if (user is not null)
            {
                user.RegisterFailedLogin(now, MaxFailedAttempts, LockoutDuration);
                await userRepository.UpdateAsync(user, ct);
            }

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
            var msg = user.Role == UserRole.Patient
                ? "Tài khoản chưa được xác thực. Vui lòng kiểm tra email để nhận mã OTP."
                : "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.";

            await activityLogService.LogAsync(
                userId: user.Id,
                userName: !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email,
                userRole: user.Role.ToString(),
                action: ActivityAction.Login,
                module: ActivityModule.Account,
                description: $"Đăng nhập thất bại — tài khoản bị vô hiệu hóa: {command.Email}",
                status: ActivityStatus.Failed,
                ipAddress: command.IpAddress,
                ct: ct);

            throw new UnauthorizedAccessException(msg);
        }

        if (command.AllowedRoles is { Length: > 0 } && !command.AllowedRoles.Contains(user.Role.ToString(), StringComparer.OrdinalIgnoreCase))
        {
            await activityLogService.LogAsync(
                userId: user.Id,
                userName: !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email,
                userRole: user.Role.ToString(),
                action: ActivityAction.Login,
                module: ActivityModule.Account,
                description: $"Đăng nhập thất bại — không đúng cổng đăng nhập: {command.Email} (role: {user.Role})",
                status: ActivityStatus.Failed,
                ipAddress: command.IpAddress,
                ct: ct);

            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");
        }

        // Đăng nhập trót lọt — xóa bộ đếm để những lần gõ nhầm rải rác qua nhiều tháng không cộng dồn
        // rồi khóa oan người dùng thật.
        if (user.FailedLoginAttempts > 0 || user.LockoutEndAt is not null)
        {
            user.ClearFailedLogins();
            await userRepository.UpdateAsync(user, ct);
        }

        var token = jwtService.GenerateToken(user);

        await activityLogService.LogAsync(
            userId: user.Id,
            userName: !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email,
            userRole: user.Role.ToString(),
            action: ActivityAction.Login,
            module: ActivityModule.Account,
            description: $"Đăng nhập thành công",
            status: ActivityStatus.Success,
            ipAddress: command.IpAddress,
            ct: ct);

        var profilePic = user.Role == UserRole.Patient
            ? user.Patient?.ProfilePictureUrl
            : user.Employee?.ProfilePictureUrl;

        return new LoginResponseDto(
            AccessToken: token,
            ExpiresIn: 15 * 60,
            User: new AuthUserDto(user.Id, user.Username ?? user.Email, user.FullName, user.Email, user.Role.ToString(), user.IsActive, profilePic));
    }

    private Task LogFailureAsync(Domain.Entities.User user, LoginCommand command, string reason, CancellationToken ct) =>
        activityLogService.LogAsync(
            userId: user.Id,
            userName: !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email,
            userRole: user.Role.ToString(),
            action: ActivityAction.Login,
            module: ActivityModule.Account,
            description: $"Đăng nhập thất bại — {reason}: {command.Email}",
            status: ActivityStatus.Failed,
            ipAddress: command.IpAddress,
            ct: ct);
}
