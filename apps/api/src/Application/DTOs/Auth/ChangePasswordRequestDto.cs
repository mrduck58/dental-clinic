namespace DentalClinic.API.Application.DTOs.Auth;

public record ChangePasswordRequestDto(
    string? CurrentPassword,
    string NewPassword);
