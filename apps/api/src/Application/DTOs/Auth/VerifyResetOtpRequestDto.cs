namespace DentalClinic.API.Application.DTOs.Auth;

public record VerifyResetOtpRequestDto(string Email, string Code);
