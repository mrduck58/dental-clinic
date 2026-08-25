using DentalClinic.API.Application.DTOs.Auth;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
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

public class FillProfileHandler(
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IDentistRepository dentistRepository) : IRequestHandler<FillProfileCommand>
{
    public async Task Handle(FillProfileCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");

        if (user.Role == UserRole.Patient)
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

            await userRepository.UpdateAsync(user, ct);
            return;
        }

        var updatedFullName = command.FullName;
        if (string.IsNullOrWhiteSpace(updatedFullName) && (!string.IsNullOrWhiteSpace(command.LastName) || !string.IsNullOrWhiteSpace(command.FirstName)))
        {
            updatedFullName = $"{command.LastName} {command.FirstName}".Trim();
        }
        if (string.IsNullOrWhiteSpace(updatedFullName))
        {
            updatedFullName = user.FullName;
        }

        user.UpdatePersonalProfile(updatedFullName ?? string.Empty, command.PhoneNumber, command.Gender);
        await userRepository.UpdateAsync(user, ct);

        var employee = user.Employee;
        if (employee == null)
        {
            employee = Employee.Create(
                user.Id, string.Empty,
                dateOfBirth: command.DateOfBirth,
                address: command.Address,
                profilePictureUrl: command.ProfilePictureUrl);
            await employeeRepository.AddAsync(employee, ct);
        }
        else
        {
            employee.Update(
                employee.Department, employee.Position, employee.EmploymentStatus, employee.EmploymentType,
                employee.StartDate,
                command.DateOfBirth ?? employee.DateOfBirth,
                command.Address ?? employee.Address,
                command.ProfilePictureUrl ?? employee.ProfilePictureUrl,
                employee.BaseSalary, employee.SalaryUnit, employee.Allowance, employee.LeaveAccrued,
                employee.RatePerShift);
            await employeeRepository.UpdateAsync(employee, ct);
        }

        if (user.Role == UserRole.Dentist)
        {
            var dentistProfile = employee.DentistProfile ?? await dentistRepository.GetByUserIdAsync(user.Id, ct);
            if (dentistProfile == null)
            {
                dentistProfile = DentistProfile.Create(
                    employee.Id,
                    command.Specialty ?? string.Empty,
                    string.Empty,
                    command.YearsOfExperience,
                    command.Education,
                    command.Bio);
                await dentistRepository.AddAsync(dentistProfile, ct);
            }
            else
            {
                dentistProfile.Update(
                    command.Specialty ?? dentistProfile.Specialization,
                    dentistProfile.LicenseNumber,
                    command.YearsOfExperience ?? dentistProfile.ExperienceYears,
                    command.Education ?? dentistProfile.Education,
                    command.Bio ?? dentistProfile.Biography,
                    dentistProfile.CertificateIssuedDate,
                    dentistProfile.CertificateIssuedBy,
                    dentistProfile.Shift);
                await dentistRepository.UpdateAsync(dentistProfile, ct);
            }
        }
    }
}
