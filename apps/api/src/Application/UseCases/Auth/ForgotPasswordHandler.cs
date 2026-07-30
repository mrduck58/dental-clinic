using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IConfiguration configuration) : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly string[] StaffRoles = ["Admin", "Owner", "Dentist", "Staff"];

    public async Task Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var email = command.Email.Trim().ToLower();
        var user = await userRepository.GetByEmailAsync(email, ct);

        // Don't reveal whether the email exists — always return success
        if (user is null || !StaffRoles.Contains(user.Role) || !user.IsActive)
            return;

        var token = GenerateSecureToken();
        user.SetPasswordResetToken(token, DateTimeOffset.UtcNow.AddHours(1));
        await userRepository.UpdateAsync(user, ct);

        var frontendBase = configuration["FrontendBaseUrl"] ?? "http://localhost:3000";
        var encodedEmail = Uri.EscapeDataString(user.Email);
        var resetLink = $"{frontendBase}/auth/reset-password?token={Uri.EscapeDataString(token)}&email={encodedEmail}";

        await emailService.SendPasswordResetAsync(
            user.Email,
            !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (user.Username ?? user.Email),
            resetLink,
            ct);
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
