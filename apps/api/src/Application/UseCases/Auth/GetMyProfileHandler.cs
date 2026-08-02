using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Auth;

public record GetMyProfileQuery(Guid UserId) : IRequest<UserProfileDto>;

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
    string? ServicesHandled,
    string? Username,
    DateTimeOffset CreatedAt
);

public class GetMyProfileHandler(IUserRepository userRepository) : IRequestHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        DateOnly? dob = user.Role switch
        {
            "Patient" => user.Patient?.DateOfBirth,
            "Dentist" => user.Dentist?.DateOfBirth,
            "Staff" => user.Staff?.DateOfBirth,
            _ => null
        };
        string? address = user.Role switch
        {
            "Patient" => user.Patient?.Address,
            "Dentist" => user.Dentist?.Address,
            "Staff" => user.Staff?.Address,
            _ => null
        };
        string? profilePic = user.Role switch
        {
            "Patient" => user.Patient?.ProfilePictureUrl,
            "Dentist" => user.Dentist?.ProfilePictureUrl,
            "Staff" => user.Staff?.ProfilePictureUrl,
            _ => null
        };
        string? employeeId = user.Role switch
        {
            "Dentist" => user.Dentist?.EmployeeId,
            "Staff" => user.Staff?.EmployeeId,
            _ => null
        };
        string? department = user.Role switch
        {
            "Dentist" => user.Dentist?.Department,
            "Staff" => user.Staff?.Department,
            _ => null
        };
        string? employmentStatus = user.Role switch
        {
            "Dentist" => user.Dentist?.EmploymentStatus,
            "Staff" => user.Staff?.EmploymentStatus,
            _ => null
        };
        string? position = user.Role switch
        {
            "Dentist" => user.Dentist?.Position,
            "Staff" => user.Staff?.Position,
            _ => null
        };
        DateOnly? startDate = user.Role switch
        {
            "Dentist" => user.Dentist?.StartDate,
            "Staff" => user.Staff?.StartDate,
            _ => null
        };
        string? education = user.Role switch
        {
            "Dentist" => user.Dentist?.Education,
            _ => null
        };
        string? bio = user.Role switch
        {
            "Dentist" => user.Dentist?.Biography,
            _ => null
        };
        DateOnly? certIssuedDate = user.Role switch
        {
            "Dentist" => user.Dentist?.CertificateIssuedDate,
            _ => null
        };
        string? certIssuedBy = user.Role switch
        {
            "Dentist" => user.Dentist?.CertificateIssuedBy,
            _ => null
        };

        decimal baseSalary = 0;
        decimal allowance = 0;
        string salaryNote = string.Empty;

        if (user.Role == "Dentist")
        {
            baseSalary = 40000000;
            allowance = (user.Dentist?.ExperienceYears ?? 0) * 2000000;
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
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            dob,
            user.Gender,
            profilePic,
            user.Role,
            employeeId,
            department,
            employmentStatus,
            position,
            startDate,
            user.Dentist?.Specialization,
            user.Dentist?.LicenseNumber,
            user.Dentist?.ExperienceYears,
            education,
            bio,
            address,
            baseSalary,
            allowance,
            salaryNote,
            certIssuedDate,
            certIssuedBy,
            null,
            user.Username,
            user.CreatedAt
        );
    }
}
