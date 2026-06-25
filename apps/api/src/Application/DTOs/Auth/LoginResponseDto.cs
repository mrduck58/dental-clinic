namespace DentalClinic.API.Application.DTOs.Auth;

public record LoginResponseDto(
    string AccessToken,
    int ExpiresIn,
    AuthUserDto User);

public record AuthUserDto(
    Guid Id,
    string Username,
    string? FullName,
    string Email,
    string Role,
    bool IsActive,
    string? ProfilePictureUrl);
