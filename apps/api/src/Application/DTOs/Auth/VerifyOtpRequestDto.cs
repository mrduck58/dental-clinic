namespace DentalClinic.API.Application.DTOs.Auth;

public record VerifyOtpRequestDto(string Email, string Code);
