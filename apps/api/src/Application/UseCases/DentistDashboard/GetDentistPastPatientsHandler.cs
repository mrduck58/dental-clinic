using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.DentistDashboard;

/// <summary>
/// GET api/appointments/dentist/patients/past — bệnh nhân đã từng khám của bác sĩ đang đăng nhập.
/// Trước đây viết THẲNG trong AppointmentsController bằng truy vấn EF; chuyển nguyên logic về handler.
/// <para>Trả <c>null</c> khi tài khoản không có hồ sơ bác sĩ — controller map thành 404 như cũ.</para>
/// </summary>
public record GetDentistPastPatientsQuery(Guid UserId) : IRequest<List<DentistPatientDto>?>;

public class GetDentistPastPatientsHandler(IDentistDashboardQueryService dentistDashboardQueryService)
    : IRequestHandler<GetDentistPastPatientsQuery, List<DentistPatientDto>?>
{
    public Task<List<DentistPatientDto>?> Handle(GetDentistPastPatientsQuery request, CancellationToken ct) =>
        dentistDashboardQueryService.GetPastPatientsAsync(request.UserId, ct);
}
