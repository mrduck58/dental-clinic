using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record FillProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string? Address = null,
    string? ProfilePictureUrl = null,
    string? Bio = null,
    string? Education = null,
    string? Specialty = null,
    int? YearsOfExperience = null) : IRequest;

public class FillProfileHandler(IUserRepository userRepository) : IRequestHandler<FillProfileCommand>
{
    public async Task Handle(FillProfileCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        if (user.Role == "Patient")
        {
            var fullName = command.FullName;
            if (string.IsNullOrWhiteSpace(fullName) && (!string.IsNullOrWhiteSpace(command.LastName) || !string.IsNullOrWhiteSpace(command.FirstName)))
            {
                fullName = $"{command.LastName} {command.FirstName}".Trim();
            }
            user.UpdatePatientProfile(
                fullName ?? string.Empty,
                command.PhoneNumber ?? string.Empty,
                command.DateOfBirth,
                command.Gender,
                command.ProfilePictureUrl);
        }
        else
        {
            var fullName = command.FullName;
            if (string.IsNullOrWhiteSpace(fullName) && (!string.IsNullOrWhiteSpace(command.LastName) || !string.IsNullOrWhiteSpace(command.FirstName)))
            {
                fullName = $"{command.LastName} {command.FirstName}".Trim();
            }
            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = user.FullName;
            }

            user.UpdatePersonalProfile(
                fullName ?? string.Empty,
                command.PhoneNumber,
                command.DateOfBirth,
                command.Gender,
                command.Address,
                command.ProfilePictureUrl,
                command.Bio,
                command.Education,
                command.Specialty,
                command.YearsOfExperience);
        }

        await userRepository.UpdateAsync(user, ct);
    }
}
