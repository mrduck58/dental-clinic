namespace DentalClinic.API.Application.DTOs.Auth;

public record FillProfileRequestDto(
    string? FirstName,
    string? LastName,
    string? FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address,
    string? ProfilePictureUrl,
    string? Bio,
    string? Education,
    string? Specialty,
    int? YearsOfExperience);
