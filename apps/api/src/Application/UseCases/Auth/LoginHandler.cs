using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Auth;

public record LoginCommand(string Email, string Password);

public class LoginHandler(IUserRepository userRepository, IJwtService jwtService)
{
    public async Task<LoginResponseDto> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, ct)
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");

        if (!BCrypt.Net.BCrypt.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        var token = jwtService.GenerateToken(user);

        return new LoginResponseDto(
            AccessToken: token,
            ExpiresIn: 15 * 60,
            User: new AuthUserDto(user.Id, user.Username, user.FullName, user.Email, user.Role, user.IsActive));
    }
}
