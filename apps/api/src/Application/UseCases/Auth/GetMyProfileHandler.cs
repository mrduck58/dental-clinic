using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Auth;

public record UserProfileDto(
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string? ProfilePictureUrl);

public class GetMyProfileHandler(IUserRepository userRepository)
{
    public async Task<UserProfileDto> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        return new UserProfileDto(
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Gender,
            user.ProfilePictureUrl);
    }
}
