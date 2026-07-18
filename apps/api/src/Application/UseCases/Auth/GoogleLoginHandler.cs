using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Auth;

public record GoogleLoginCommand(string IdToken);

public class GoogleLoginHandler(
    IGoogleAuthService googleAuthService,
    IUserRepository userRepository,
    IJwtService jwtService)
{
    public async Task<GoogleLoginResponseDto> HandleAsync(GoogleLoginCommand command, CancellationToken ct = default)
    {
        var googleUser = await googleAuthService.VerifyIdTokenAsync(command.IdToken, ct);

        var user = await userRepository.GetByEmailAsync(googleUser.Email, ct);
        var isNewUser = user is null;

        if (user is null)
        {
            user = User.CreateGoogleUser(googleUser.Email, googleUser.FullName, googleUser.PictureUrl);
            await userRepository.AddAsync(user, ct);
        }
        else if (user.Role != "Patient")
        {
            throw new UnauthorizedAccessException("Tài khoản này không thể đăng nhập bằng ứng dụng di động.");
        }
        else if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
        }

        var token = jwtService.GenerateToken(user);

        var profilePic = user.Role switch
        {
            "Patient" => user.Patient?.ProfilePictureUrl,
            "Dentist" => user.Dentist?.ProfilePictureUrl,
            "Staff" => user.Staff?.ProfilePictureUrl,
            _ => null
        };

        return new GoogleLoginResponseDto(
            AccessToken: token,
            ExpiresIn: 15 * 60,
            IsNewUser: isNewUser,
            User: new AuthUserDto(user.Id, user.Username ?? user.Email, user.FullName, user.Email, user.Role, user.IsActive, profilePic));
    }
}
