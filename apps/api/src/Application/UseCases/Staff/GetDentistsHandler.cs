using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Staff;

public record DentistSummaryDto(
    Guid Id,
    string? FullName,
    string? Specialty,
    string? ProfilePictureUrl,
    int? YearsOfExperience,
    string? Bio);

public class GetDentistsHandler(IUserRepository userRepository)
{
    public async Task<IEnumerable<DentistSummaryDto>> HandleAsync(CancellationToken ct = default)
    {
        var (items, _) = await userRepository.GetStaffPagedAsync(
            search: null, role: null, status: "active", page: 1, pageSize: 50, ct);

        return items
            .Where(u => u.Role is "Dentist" or "Doctor")
            .Select(u => new DentistSummaryDto(
                u.Id, u.FullName, u.Specialty, u.ProfilePictureUrl, u.YearsOfExperience, u.Bio));
    }
}
