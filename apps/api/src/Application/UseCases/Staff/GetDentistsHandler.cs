using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Staff;

public record DentistSummaryDto(
    Guid Id,
    string? FullName,
    string? Specialty,
    string? ProfilePictureUrl,
    int? YearsOfExperience,
    string? Bio);

public class GetDentistsHandler(IUserRepository userRepository, AppDbContext? dbContext = null)
{
    public async Task<IEnumerable<DentistSummaryDto>> HandleAsync(CancellationToken ct = default)
    {
        if (dbContext != null)
        {
            var dbDentists = await dbContext.Dentists
                .AsNoTracking()
                .Include(d => d.User)
                .ToListAsync(ct);

            if (dbDentists.Count > 0)
            {
                return dbDentists.Select(d => new DentistSummaryDto(
                    d.Id,
                    d.FullName,
                    d.Specialization,
                    d.ProfilePictureUrl,
                    d.ExperienceYears,
                    d.Biography));
            }
        }

        var (items, _) = await userRepository.GetStaffPagedAsync(
            search: null, role: null, status: null, page: 1, pageSize: 500, ct);

        return items
            .Where(u =>
                string.Equals(u.Role, "Dentist", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.Role, "Doctor", StringComparison.OrdinalIgnoreCase) ||
                u.Dentist != null)
            .Select(u => new DentistSummaryDto(
                u.Dentist?.Id ?? u.Id,
                u.FullName,
                u.Dentist?.Specialization ?? "Nha khoa tổng quát",
                u.Dentist?.ProfilePictureUrl,
                u.Dentist?.ExperienceYears ?? 5,
                u.Dentist?.Biography));
    }
}
