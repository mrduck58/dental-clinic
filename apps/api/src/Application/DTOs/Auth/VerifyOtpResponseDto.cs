namespace DentalClinic.API.Application.DTOs.Auth;

public record VerifyOtpResponseDto(
    Guid Id,
    string Email,
    string Role,
    string AccessToken,
    int ExpiresIn);
