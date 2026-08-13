using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.DentistDashboard;

/// <summary>
/// GET api/appointments/dentist/patients — bệnh nhân trong ngày của CHÍNH bác sĩ đang đăng nhập.
/// <para>
/// Trước đây AppointmentsController tự truy vấn <c>dbContext.Dentists</c> để đổi userId → dentistId
/// và tự tính ngày mặc định theo giờ VN trước khi gọi handler. Toàn bộ logic (đổi userId → dentistId,
/// tính ngày mặc định, rồi truy vấn danh sách bệnh nhân trong ngày — cùng logic với
/// <see cref="GetDentistPatientsQuery"/>) nay nằm trong <see cref="IDentistDashboardQueryService.GetMyPatientsAsync"/>.
/// </para>
/// <para>Trả <c>null</c> khi tài khoản không có hồ sơ bác sĩ — controller map thành 404 như cũ.</para>
/// </summary>
public record GetMyDentistPatientsQuery(Guid UserId, DateOnly? Date) : IRequest<DentistPatientsResponse?>;

public class GetMyDentistPatientsHandler(IDentistDashboardQueryService dentistDashboardQueryService)
    : IRequestHandler<GetMyDentistPatientsQuery, DentistPatientsResponse?>
{
    public Task<DentistPatientsResponse?> Handle(GetMyDentistPatientsQuery request, CancellationToken ct) =>
        dentistDashboardQueryService.GetMyPatientsAsync(request.UserId, request.Date, ct);
}
