using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Staff;

public record CreateStaffCommand(
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    string? EmployeeId,
    string? Department,
    string? EmploymentStatus,
    string? ProfilePictureUrl,
    string? ProfessionalNotes,
    string? Specialty,
    string? LicenseNumber,
    int? YearsOfExperience,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Address,
    DateOnly? StartDate,
    string? ServicesHandled,
    DateOnly? CertificateIssuedDate,
    string? CertificateIssuedBy,
    string? Education,
    string? Bio,
    string? Position);

public class CreateStaffHandler(IUserRepository userRepository)
{
    public async Task<StaffItemDto> HandleAsync(CreateStaffCommand command, CancellationToken ct = default)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new ConflictException($"Email '{command.Email}' đã được sử dụng bởi tài khoản khác.");

        var user = User.CreateEmployee(command.Email, command.Role, command.PhoneNumber, command.FullName);

        user.SetStaffProfile(new StaffProfileData(
            command.EmployeeId, command.Department,
            command.EmploymentStatus, command.ProfilePictureUrl, command.ProfessionalNotes,
            command.Specialty, command.LicenseNumber, command.YearsOfExperience,
            command.Gender, command.DateOfBirth, command.Address,
            command.StartDate, command.ServicesHandled, command.CertificateIssuedDate,
            command.CertificateIssuedBy, command.Education, command.Bio, command.Position));

        await userRepository.AddAsync(user, ct);

        return GetStaffHandler.ToDto(user);
    }
}
