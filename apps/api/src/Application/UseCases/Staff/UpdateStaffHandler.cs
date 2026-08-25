using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Validators;
using MediatR;

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
    string? Position,
    string? EmploymentType,
    decimal? BaseSalary,
    string? SalaryUnit,
    decimal? LeaveAccrued,
    decimal? Allowance,
    decimal? RatePerShift = null,
    string? Content = null) : IRequest<StaffItemDto>;

public class UpdateStaffHandler(
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IDentistRepository dentistRepository)
    : IRequestHandler<UpdateStaffCommand, StaffItemDto>
{
    public async Task<StaffItemDto> Handle(UpdateStaffCommand command, CancellationToken ct)
    {
        // Validate all fields
        StaffValidator.ValidateUpdate(
            command.FullName, command.Email, command.PhoneNumber, command.Role,
            command.Gender, command.DateOfBirth, command.Address,
            command.Specialty, command.LicenseNumber, command.YearsOfExperience,
            command.StartDate, command.ServicesHandled,
            command.CertificateIssuedDate, command.CertificateIssuedBy,
            command.Education, command.Bio, command.Position, command.Department,
            command.EmploymentType, command.BaseSalary, command.SalaryUnit,
            command.LeaveAccrued, command.EmploymentStatus);

        var user = await userRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException($"Không tìm thấy nhân viên với ID '{command.Id}'.");

        if (!string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (await userRepository.ExistsByEmailAsync(command.Email, ct))
                throw new ConflictException($"Email '{command.Email}' đã được sử dụng bởi tài khoản khác.");
        }

        if (!Enum.TryParse<UserRole>(command.Role, true, out var role))
            throw new ValidationException($"Vai trò '{command.Role}' không hợp lệ.");

        user.Update(command.FullName, command.Email, command.PhoneNumber, role, command.IsActive, command.Gender);
        await userRepository.UpdateAsync(user, ct);

        var employee = user.Employee;
        if (employee == null)
        {
            employee = Employee.Create(user.Id, string.Empty);
            await employeeRepository.AddAsync(employee, ct);
        }
        employee.Update(
            command.Department, command.Position,
            command.EmploymentStatus ?? Employee.DefaultEmploymentStatus,
            command.EmploymentType, command.StartDate, command.DateOfBirth,
            command.Address, command.ProfilePictureUrl,
            command.BaseSalary, command.SalaryUnit, command.Allowance, command.LeaveAccrued, command.RatePerShift);
        await employeeRepository.UpdateAsync(employee, ct);

        if (role == UserRole.Dentist)
        {
            var dentistProfile = employee.DentistProfile ?? await dentistRepository.GetByUserIdAsync(user.Id, ct);
            if (dentistProfile == null)
            {
                dentistProfile = DentistProfile.Create(
                    employee.Id,
                    command.Specialty ?? string.Empty,
                    command.LicenseNumber ?? string.Empty,
                    command.YearsOfExperience,
                    command.Education, command.Bio,
                    command.CertificateIssuedDate, command.CertificateIssuedBy,
                    content: command.Content);
                await dentistRepository.AddAsync(dentistProfile, ct);
            }
            else
            {
                dentistProfile.Update(
                    command.Specialty ?? string.Empty,
                    command.LicenseNumber ?? string.Empty,
                    command.YearsOfExperience,
                    command.Education, command.Bio,
                    command.CertificateIssuedDate, command.CertificateIssuedBy,
                    dentistProfile.Shift,
                    command.Content);
                await dentistRepository.UpdateAsync(dentistProfile, ct);
            }
        }

        var updated = await userRepository.GetByIdAsync(user.Id, ct) ?? user;
        return GetStaffHandler.ToDto(updated);
    }
}
