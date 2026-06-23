namespace DentalClinic.API.Application.DTOs.Auth;

public record FillProfileRequestDto(
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender);
