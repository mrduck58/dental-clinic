using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Staff;

public record GetStaffQuery(
    string? Search,
    string? Role,
    string? Status,
    int Page,
    int PageSize);

public record StaffItemDto(
    Guid Id,
    string Username,
    string Email,
    string Role,
    string? FullName,
    string? PhoneNumber,
    bool IsActive,
    string? EmployeeId,
    string? Department,
    string? EmploymentStatus,
    string? ProfilePictureUrl,
    string? ProfessionalNotes,
    DateTimeOffset CreatedAt,
    string? Specialty,
    string? LicenseNumber,
    int? YearsOfExperience,
    bool HasAccount,
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
    decimal? LeaveAccrued);

public record StaffStatsDto(int TotalEmployees, int TotalDentists, int TotalDoctors);

public record StaffPagedResult(
    IReadOnlyList<StaffItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    StaffStatsDto Statistics);

public class GetStaffHandler(IUserRepository userRepository)
{
    public async Task<StaffPagedResult> HandleAsync(GetStaffQuery query, CancellationToken ct = default)
    {
        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, total) = await userRepository.GetStaffPagedAsync(
            query.Search, query.Role, query.Status, page, pageSize, ct);

        var stats = await userRepository.GetStaffStatsAsync(ct);

        return new StaffPagedResult(
            Items: items.Select(ToDto).ToList(),
            TotalCount: total,
            Page: page,
            PageSize: pageSize,
            Statistics: new StaffStatsDto(stats.TotalEmployees, stats.TotalDentists, stats.TotalDoctors));
    }

    /// <summary>Get a single staff member by ID.</summary>
    public async Task<StaffItemDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Không tìm thấy nhân viên với ID '{id}'.");

        return ToDto(user);
    }

    public static StaffItemDto ToDto(User u) => new(
        u.Id, u.Username ?? "", u.Email, u.Role,
        u.FullName, u.PhoneNumber, u.IsActive,
        u.EmployeeId, u.Department, u.EmploymentStatus,
        u.ProfilePictureUrl, u.ProfessionalNotes, u.CreatedAt,
        u.Specialty, u.LicenseNumber, u.YearsOfExperience, u.HasAccount,
        u.Gender, u.DateOfBirth, u.Address,
        u.StartDate, u.ServicesHandled, u.CertificateIssuedDate,
        u.CertificateIssuedBy, u.Education, u.Bio, u.Position,
        u.EmploymentType, u.BaseSalary, u.SalaryUnit, u.LeaveAccrued);
}
