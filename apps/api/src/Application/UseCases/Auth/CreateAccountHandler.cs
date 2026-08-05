using System.Security.Cryptography;
using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Domain.Constants;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record CreateAccountCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Role) : IRequest<CreateAccountResponseDto>;

public class CreateAccountHandler(IUserRepository userRepository, IEmailService emailService, IActivityLogService activityLogService, ICurrentUserService currentUser) : IRequestHandler<CreateAccountCommand, CreateAccountResponseDto>
{
    public async Task<CreateAccountResponseDto> Handle(CreateAccountCommand command, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new ConflictException($"Email '{command.Email}' đã được sử dụng bởi tài khoản khác.");

        var username = BuildUsername(command.Email);

        if (await userRepository.ExistsByUsernameAsync(username, ct))
            username = $"{username}_{RandomNumberGenerator.GetHexString(4, lowercase: true)}";

        if (!Enum.TryParse<UserRole>(command.Role, true, out var role))
            throw new ValidationException($"Vai trò '{command.Role}' không hợp lệ.");

        var rawPassword = GenerateSecurePassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);

        var user = User.Create(username, command.Email, passwordHash, role, command.PhoneNumber, command.FullName);
        await userRepository.AddAsync(user, ct);

        await emailService.SendStaffCredentialsAsync(command.Email, command.FullName, rawPassword, ct);

        await activityLogService.LogAsync(
            userId: currentUser.UserId,
            userName: currentUser.UserName,
            userRole: currentUser.UserRole,
            action: ActivityAction.Create,
            module: ActivityModule.Account,
            description: $"Tạo tài khoản mới: {command.FullName} ({command.Email}) - Role: {command.Role}",
            status: ActivityStatus.Success,
            ipAddress: currentUser.IpAddress,
            targetId: user.Id.ToString(),
            ct: ct);

        return new CreateAccountResponseDto(user.Id, user.Username!, user.Email, user.Role.ToString(), user.CreatedAt);
    }

    private static string BuildUsername(string email) =>
        email.Split('@')[0]
             .ToLowerInvariant()
             .Replace('.', '_')
             .Replace('-', '_');

    private static string GenerateSecurePassword(int length = 8)
    {
        const string upper   = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower   = "abcdefghijklmnopqrstuvwxyz";
        const string digits  = "0123456789";
        const string special = "!@#$%&";
        const string all     = upper + lower + digits + special;

        var pw = new char[length];
        var b  = RandomNumberGenerator.GetBytes(length + 4);

        // Guarantee at least one character from each category
        pw[0] = upper  [b[0] % upper.Length];
        pw[1] = lower  [b[1] % lower.Length];
        pw[2] = digits [b[2] % digits.Length];
        pw[3] = special[b[3] % special.Length];

        for (int i = 4; i < length; i++)
            pw[i] = all[b[i] % all.Length];

        // Fisher-Yates shuffle
        var s = RandomNumberGenerator.GetBytes(length);
        for (int i = length - 1; i > 0; i--)
        {
            int j = s[i] % (i + 1);
            (pw[i], pw[j]) = (pw[j], pw[i]);
        }

        return new string(pw);
    }
}
