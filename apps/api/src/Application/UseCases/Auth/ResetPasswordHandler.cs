using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;

public class ResetPasswordHandler(IUserRepository userRepository) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var email = command.Email.Trim().ToLower();
        var user = await userRepository.GetByEmailAsync(email, ct)
            ?? throw new UnauthorizedAccessException("Liên kết không hợp lệ hoặc đã hết hạn.");

        if (user.PasswordResetToken is null
            || user.PasswordResetToken != command.Token
            || user.PasswordResetTokenExpiry < DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Liên kết không hợp lệ hoặc đã hết hạn.");
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword, workFactor: 12);
        user.ResetPassword(newHash); // also clears token + expiry
        await userRepository.UpdateAsync(user, ct);
    }
}
