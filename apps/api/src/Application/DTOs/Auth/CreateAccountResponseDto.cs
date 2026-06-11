namespace DentalClinic.API.Application.DTOs.Auth;

public record CreateAccountResponseDto(
    Guid Id,
    string Username,
    string Email,
    string Role,
    DateTimeOffset CreatedAt);
