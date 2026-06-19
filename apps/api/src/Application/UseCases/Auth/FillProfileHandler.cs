using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Auth;

public record FillProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender);

public class FillProfileHandler(IUserRepository userRepository)
{
    public async Task HandleAsync(FillProfileCommand command, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        var fullName = $"{command.LastName} {command.FirstName}".Trim();
        user.UpdatePatientProfile(fullName, command.PhoneNumber, command.DateOfBirth, command.Gender);

        await userRepository.UpdateAsync(user, ct);
    }
}
