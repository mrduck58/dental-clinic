using DentalClinic.API.Application.DTOs.Patients;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Patients;

/// <summary>OnlyWithoutAccount=true bỏ qua ràng buộc tối thiểu 2 ký tự — dùng để duyệt danh sách
/// bệnh nhân chưa có tài khoản (staff bấm chọn nhanh), không phải tìm kiếm chủ động.</summary>
public record SearchPatientsQuery(string? Q, int Limit, bool OnlyWithoutAccount = false) : IRequest<IEnumerable<PatientSearchResultDto>>;

public class SearchPatientsHandler(IPatientRepository patientRepository)
    : IRequestHandler<SearchPatientsQuery, IEnumerable<PatientSearchResultDto>>
{
    public async Task<IEnumerable<PatientSearchResultDto>> Handle(SearchPatientsQuery query, CancellationToken ct)
    {
        var term = (query.Q ?? string.Empty).Trim();
        if (term.Length < 2 && !query.OnlyWithoutAccount) return [];

        var take = query.Limit is > 0 and <= 20 ? query.Limit : 8;
        var patients = await patientRepository.SearchAsync(term, take, query.OnlyWithoutAccount, ct);

        return patients.Select(p => new PatientSearchResultDto(
            p.Id,
            p.FullName,
            p.PhoneNumber ?? p.User?.PhoneNumber,
            p.DateOfBirth,
            p.Gender,
            p.User != null && p.User.PasswordHash != null
        ));
    }
}
