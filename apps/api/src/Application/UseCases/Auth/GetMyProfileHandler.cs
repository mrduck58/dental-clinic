using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Auth;

public record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? Gender,
    string? ProfilePictureUrl,
    string Role,
    string? EmployeeId,
    string? Department,
    string? EmploymentStatus,
    string? Position,
    DateOnly? StartDate,
    string? Specialty,
    string? LicenseNumber,
    int? YearsOfExperience,
    string? Education,
    string? Bio,
    string? Address,
    decimal BaseSalary,
    decimal Allowance,
    string SalaryNote,
    DateOnly? CertificateIssuedDate,
    string? CertificateIssuedBy,
    string? ServicesHandled
);

public class GetMyProfileHandler(IUserRepository userRepository)
{
    public async Task<UserProfileDto> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        decimal baseSalary = 0;
        decimal allowance = 0;
        string salaryNote = string.Empty;

        if (user.Role == "Dentist")
        {
            baseSalary = 40000000;
            allowance = (user.YearsOfExperience ?? 0) * 2000000;
            salaryNote = "Lương cơ bản bác sĩ + Phụ cấp theo số ca điều trị thực tế (phụ cấp chuyên môn)";
        }
        else if (user.Role == "Admin")
        {
            baseSalary = 25000000;
            allowance = 5000000;
            salaryNote = "Lương cơ bản quản lý + Phụ cấp thâm niên";
        }
        else if (user.Role == "Staff")
        {
            baseSalary = 12000000;
            allowance = 2000000;
            salaryNote = "Lương cơ bản hành chính + Phụ cấp tăng ca/phụ cấp trách nhiệm";
        }

        return new UserProfileDto(
            user.Id,
            user.FullName ?? string.Empty,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth,
            user.Gender,
            user.ProfilePictureUrl,
            user.Role,
            user.EmployeeId,
            user.Department,
            user.EmploymentStatus,
            user.Position,
            user.StartDate,
            user.Specialty,
            user.LicenseNumber,
            user.YearsOfExperience,
            user.Education,
            user.Bio,
            user.Address,
            baseSalary,
            allowance,
            salaryNote,
            user.CertificateIssuedDate,
            user.CertificateIssuedBy,
            user.ServicesHandled
        );
    }
}
