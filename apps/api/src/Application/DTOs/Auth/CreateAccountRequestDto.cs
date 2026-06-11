namespace DentalClinic.API.Application.DTOs.Auth;

public record CreateAccountRequestDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Role);
