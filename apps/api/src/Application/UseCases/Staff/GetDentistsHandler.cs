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
        // Lấy toàn bộ nhân sự rồi lọc trong bộ nhớ KHÔNG phân biệt hoa/thường:
        // dữ liệu thực tế trong DB có role "doctor"/"Dentist" và status "Active"
        // với cách viết hoa khác nhau, nên không thể dựa vào filter so khớp tuyệt đối ở DB.
        var (items, _) = await userRepository.GetStaffPagedAsync(
            search: null, role: null, status: null, page: 1, pageSize: 500, ct);

        return items
            .Where(u =>
                (string.Equals(u.Role, "Dentist", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.Role, "Doctor", StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(u.Dentist?.EmploymentStatus ?? "Active", "Active", StringComparison.OrdinalIgnoreCase))
            .Select(u => new DentistSummaryDto(
                u.Id, u.FullName, u.Dentist?.Specialization, u.Dentist?.ProfilePictureUrl, u.Dentist?.ExperienceYears, u.Dentist?.Biography));
    }
}
