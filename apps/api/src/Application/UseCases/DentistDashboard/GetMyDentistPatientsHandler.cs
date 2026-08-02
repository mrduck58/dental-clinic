using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.DentistDashboard;

/// <summary>
/// GET api/appointments/dentist/patients — bệnh nhân trong ngày của CHÍNH bác sĩ đang đăng nhập.
/// <para>
/// Trước đây AppointmentsController tự truy vấn <c>dbContext.Dentists</c> để đổi userId → dentistId
/// và tự tính ngày mặc định theo giờ VN trước khi gọi handler. Chuyển hết vào handler này để
/// controller không còn chạm <c>AppDbContext</c>; phần truy vấn danh sách vẫn dùng lại
/// <see cref="GetDentistPatientsQuery"/> (có test riêng, giữ nguyên chữ ký).
/// </para>
/// <para>Trả <c>null</c> khi tài khoản không có hồ sơ bác sĩ — controller map thành 404 như cũ.</para>
/// </summary>
public record GetMyDentistPatientsQuery(Guid UserId, DateOnly? Date) : IRequest<DentistPatientsResponse?>;

public class GetMyDentistPatientsHandler(AppDbContext dbContext, ISender sender)
    : IRequestHandler<GetMyDentistPatientsQuery, DentistPatientsResponse?>
{
    private static readonly TimeZoneInfo VietnamTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public async Task<DentistPatientsResponse?> Handle(GetMyDentistPatientsQuery request, CancellationToken ct)
    {
        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == request.UserId, ct);
        if (dentist == null) return null;

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var queryDate = request.Date ?? DateOnly.FromDateTime(vietnamNow);

        return await sender.Send(new GetDentistPatientsQuery(dentist.Id, queryDate), ct);
    }
}
