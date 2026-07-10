namespace DentalClinic.API.Application.DTOs.Auth;

public record GoogleLoginResponseDto(
    string AccessToken,
    int ExpiresIn,
    bool IsNewUser,
    AuthUserDto User);
