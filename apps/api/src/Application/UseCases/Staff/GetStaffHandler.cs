using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Application.UseCases.Staff;

public record GetStaffQuery(
    string? Search,
    string? Role,
    string? Status,
    string? Specialty,
    int Page,
    int PageSize,
    string? SortBy = null,
    string? SortDir = null);

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
    decimal? LeaveAccrued,
    decimal? Allowance);

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
            query.Search, query.Role, query.Status, query.Specialty, page, pageSize,
            query.SortBy, query.SortDir, ct);

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

    public static StaffItemDto ToDto(User u)
    {
        var e = u.Employee;
        var d = e?.DentistProfile;
        return new StaffItemDto(
            u.Id, u.Username ?? "", u.Email ?? "", u.Role.ToString(),
            u.FullName, u.PhoneNumber, u.IsActive,
            e?.EmployeeId, e?.Department, e?.EmploymentStatus,
            e?.ProfilePictureUrl, null, u.CreatedAt,
            d?.Specialization, d?.LicenseNumber, d?.ExperienceYears, u.HasAccount,
            u.Gender, e?.DateOfBirth, e?.Address,
            e?.StartDate, null, d?.CertificateIssuedDate,
            d?.CertificateIssuedBy, d?.Education, d?.Biography, e?.Position,
            e?.EmploymentType, e?.BaseSalary, e?.SalaryUnit, e?.LeaveAccrued, e?.Allowance);
    }
}
