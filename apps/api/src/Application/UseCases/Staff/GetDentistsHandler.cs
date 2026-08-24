using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Staff;

public record DentistSummaryDto(
    Guid Id,
    string? FullName,
    string? Specialty,
    string? ProfilePictureUrl,
    int? YearsOfExperience,
    string? Bio,
    /// <summary>Bài viết chi tiết (HTML) giới thiệu về nha sĩ — hiển thị trên clinic_website.</summary>
    string? Content);

public record GetDentistsQuery : IRequest<IEnumerable<DentistSummaryDto>>;

public class GetDentistsHandler(IUserRepository userRepository, IDentistRepository dentistRepository)
    : IRequestHandler<GetDentistsQuery, IEnumerable<DentistSummaryDto>>
{
    public async Task<IEnumerable<DentistSummaryDto>> Handle(GetDentistsQuery request, CancellationToken ct)
    {
        var dbDentists = await dentistRepository.GetAllWithUserAsync(ct);

        if (dbDentists.Count > 0)
        {
            return dbDentists.Select(d => new DentistSummaryDto(
                d.Id,
                d.FullName,
                d.Specialization,
                d.ProfilePictureUrl,
                d.ExperienceYears,
                d.Biography,
                d.Content));
        }

        var (items, _) = await userRepository.GetStaffPagedAsync(
            search: null, role: null, status: null, specialty: null, page: 1, pageSize: 500, ct: ct);

        return items
            .Where(u => u.Role == UserRole.Dentist || u.Employee?.DentistProfile != null)
            .Select(u => new DentistSummaryDto(
                u.Employee?.DentistProfile?.Id ?? u.Id,
                u.FullName,
                u.Employee?.DentistProfile?.Specialization ?? "Nha khoa tổng quát",
                u.Employee?.ProfilePictureUrl,
                u.Employee?.DentistProfile?.ExperienceYears ?? 5,
                u.Employee?.DentistProfile?.Biography,
                u.Employee?.DentistProfile?.Content));
    }
}
