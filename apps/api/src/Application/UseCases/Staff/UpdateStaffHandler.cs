using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Staff;

public record UpdateStaffCommand(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string Role,
    string? Department,
    string? EmploymentStatus,
    string? ProfilePictureUrl,
    string? ProfessionalNotes,
    bool IsActive,
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

public class UpdateStaffHandler(IUserRepository userRepository)
{
    public async Task<StaffItemDto> HandleAsync(UpdateStaffCommand command, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy nhân viên với ID '{command.Id}'.");

        if (!string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await userRepository.ExistsByEmailAsync(command.Email, ct))
                throw new ConflictException($"Email '{command.Email}' đã được sử dụng bởi tài khoản khác.");
        }

        user.Update(new UpdateStaffData(
            command.FullName, command.Email, command.PhoneNumber, command.Role,
            command.Department, command.EmploymentStatus,
            command.ProfilePictureUrl, command.ProfessionalNotes, command.IsActive,
            command.Specialty, command.LicenseNumber, command.YearsOfExperience,
            command.Gender, command.DateOfBirth, command.Address,
            command.StartDate, command.ServicesHandled, command.CertificateIssuedDate,
            command.CertificateIssuedBy, command.Education, command.Bio, command.Position));

        await userRepository.UpdateAsync(user, ct);

        return GetStaffHandler.ToDto(user);
    }
}
