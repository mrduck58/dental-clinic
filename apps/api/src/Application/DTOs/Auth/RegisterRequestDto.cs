namespace DentalClinic.API.Application.DTOs.Auth;

public record RegisterRequestDto(string Email, string Password, string ConfirmPassword);
