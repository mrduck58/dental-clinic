using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Validators;
using MediatR;

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
    string? Position,
    string? EmploymentType,
    decimal? BaseSalary,
    string? SalaryUnit,
    decimal? LeaveAccrued,
    decimal? Allowance) : IRequest<StaffItemDto>;

public class CreateStaffHandler(
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IDentistRepository dentistRepository)
    : IRequestHandler<CreateStaffCommand, StaffItemDto>
{
    public async Task<StaffItemDto> Handle(CreateStaffCommand command, CancellationToken ct)
    {
        // Validate all fields
        StaffValidator.ValidateCreate(
            command.FullName, command.Email, command.PhoneNumber, command.Role,
            command.Gender, command.DateOfBirth, command.Address,
            command.Specialty, command.LicenseNumber, command.YearsOfExperience,
            command.StartDate, command.ServicesHandled,
            command.CertificateIssuedDate, command.CertificateIssuedBy,
            command.Education, command.Bio, command.Position, command.Department,
            command.EmploymentType, command.BaseSalary, command.SalaryUnit,
            command.LeaveAccrued, command.EmploymentStatus);

        if (await userRepository.ExistsByEmailAsync(command.Email, ct))
            throw new ConflictException($"Email '{command.Email}' đã được sử dụng bởi tài khoản khác.");

        if (!Enum.TryParse<UserRole>(command.Role, true, out var role))
            throw new ValidationException($"Vai trò '{command.Role}' không hợp lệ.");

        var user = User.CreateEmployee(command.Email, role, command.PhoneNumber, command.FullName);
        user.UpdateGender(command.Gender);
        await userRepository.AddAsync(user, ct);

        var employee = Employee.Create(
            user.Id,
            command.EmployeeId ?? string.Empty,
            command.Department,
            command.Position,
            command.EmploymentStatus ?? Employee.DefaultEmploymentStatus,
            command.EmploymentType,
            command.StartDate,
            command.DateOfBirth,
            command.Address,
            command.ProfilePictureUrl,
            command.BaseSalary,
            command.SalaryUnit,
            command.Allowance,
            command.LeaveAccrued);
        await employeeRepository.AddAsync(employee, ct);

        if (role == UserRole.Dentist)
        {
            var dentistProfile = DentistProfile.Create(
                employee.Id,
                command.Specialty ?? string.Empty,
                command.LicenseNumber ?? string.Empty,
                command.YearsOfExperience,
                command.Education,
                command.Bio,
                command.CertificateIssuedDate,
                command.CertificateIssuedBy);
            await dentistRepository.AddAsync(dentistProfile, ct);
        }

        var created = await userRepository.GetByIdAsync(user.Id, ct) ?? user;
        return GetStaffHandler.ToDto(created);
    }
}
