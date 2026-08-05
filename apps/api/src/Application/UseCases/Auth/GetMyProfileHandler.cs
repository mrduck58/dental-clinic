using DentalClinic.API.Domain.Enums;
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

        var employee = user.Employee;
        var dentist = employee?.DentistProfile;
        var isPatient = user.Role == UserRole.Patient;

        DateOnly? dob = isPatient ? user.Patient?.DateOfBirth : employee?.DateOfBirth;
        string? address = isPatient ? user.Patient?.Address : employee?.Address;
        string? profilePic = isPatient ? user.Patient?.ProfilePictureUrl : employee?.ProfilePictureUrl;

        string? employeeId = employee?.EmployeeId;
        string? department = employee?.Department;
        string? employmentStatus = employee?.EmploymentStatus;
        string? position = employee?.Position;
        DateOnly? startDate = employee?.StartDate;
        string? education = dentist?.Education;
        string? bio = dentist?.Biography;
        DateOnly? certIssuedDate = dentist?.CertificateIssuedDate;
        string? certIssuedBy = dentist?.CertificateIssuedBy;

        decimal baseSalary = 0;
        decimal allowance = 0;
        string salaryNote = string.Empty;

        if (user.Role == UserRole.Dentist)
        {
            baseSalary = 40000000;
            allowance = (dentist?.ExperienceYears ?? 0) * 2000000;
            salaryNote = "Lương cơ bản bác sĩ + Phụ cấp theo số ca điều trị thực tế (phụ cấp chuyên môn)";
        }
        else if (user.Role == UserRole.Admin)
        {
            baseSalary = 25000000;
            allowance = 5000000;
            salaryNote = "Lương cơ bản quản lý + Phụ cấp thâm niên";
        }
        else if (user.Role == UserRole.Staff)
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
            user.Role.ToString(),
            employeeId,
            department,
            employmentStatus,
            position,
            startDate,
            dentist?.Specialization,
            dentist?.LicenseNumber,
            dentist?.ExperienceYears,
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
