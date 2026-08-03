using System.Security.Cryptography;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Staff;

public record ResetStaffPasswordResult(Guid Id, string Email, string TemporaryPassword);

public record ResetStaffPasswordCommand(Guid StaffId) : IRequest<ResetStaffPasswordResult>;

public class ResetStaffPasswordHandler(IUserRepository userRepository, IEmailService emailService)
    : IRequestHandler<ResetStaffPasswordCommand, ResetStaffPasswordResult>
{
    public async Task<ResetStaffPasswordResult> Handle(ResetStaffPasswordCommand request, CancellationToken ct)
    {
        var staffId = request.StaffId;
        var user = await userRepository.GetByIdAsync(staffId, ct)
            ?? throw new NotFoundException($"Không tìm thấy nhân viên với ID '{staffId}'.");

        var rawPassword  = GenerateSecurePassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);

        user.ResetPassword(passwordHash);
        await userRepository.UpdateAsync(user, ct);

        await emailService.SendStaffCredentialsAsync(user.Email, !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Username ?? user.Email), rawPassword, ct);

        return new ResetStaffPasswordResult(user.Id, user.Email, rawPassword);
    }

    private static string GenerateSecurePassword(int length = 8)
    {
        const string upper   = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower   = "abcdefghijklmnopqrstuvwxyz";
        const string digits  = "0123456789";
        const string special = "!@#$%&";
        const string all     = upper + lower + digits + special;

        var pw = new char[length];
        var b  = RandomNumberGenerator.GetBytes(length + 4);

        pw[0] = upper  [b[0] % upper.Length];
        pw[1] = lower  [b[1] % lower.Length];
        pw[2] = digits [b[2] % digits.Length];
        pw[3] = special[b[3] % special.Length];

        for (int i = 4; i < length; i++)
            pw[i] = all[b[i] % all.Length];

        var s = RandomNumberGenerator.GetBytes(length);
        for (int i = length - 1; i > 0; i--)
        {
            int j = s[i] % (i + 1);
            (pw[i], pw[j]) = (pw[j], pw[i]);
        }

        return new string(pw);
    }
}
