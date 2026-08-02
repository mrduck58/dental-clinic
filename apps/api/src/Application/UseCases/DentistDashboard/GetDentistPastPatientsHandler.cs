using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.DentistDashboard;

/// <summary>
/// GET api/appointments/dentist/patients/past — bệnh nhân đã từng khám của bác sĩ đang đăng nhập.
/// Trước đây viết THẲNG trong AppointmentsController bằng truy vấn EF; chuyển nguyên logic về handler.
/// <para>Trả <c>null</c> khi tài khoản không có hồ sơ bác sĩ — controller map thành 404 như cũ.</para>
/// </summary>
public record GetDentistPastPatientsQuery(Guid UserId) : IRequest<List<DentistPatientDto>?>;

public class GetDentistPastPatientsHandler(AppDbContext dbContext)
    : IRequestHandler<GetDentistPastPatientsQuery, List<DentistPatientDto>?>
{
    public async Task<List<DentistPatientDto>?> Handle(GetDentistPastPatientsQuery request, CancellationToken ct)
    {
        var dentist = await dbContext.Dentists.FirstOrDefaultAsync(d => d.UserId == request.UserId, ct);
        if (dentist == null) return null;

        var appointments = await dbContext.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.User)
            .Include(a => a.Service)
            .Where(a => a.DentistId == dentist.Id &&
                        (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.PendingPayment))
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(ct);

        return appointments.Select(a => new DentistPatientDto(
            a.Id,
            $"DK{a.AppointmentDate:yyyyMMdd}{a.Id.ToString("N")[..6].ToUpper()}",
            a.Patient.FullName,
            DentistPatientMapper.CalculateAge(a.Patient.DateOfBirth),
            a.Patient.Gender ?? "Khác",
            a.Patient.PhoneNumber ?? a.Patient.User?.PhoneNumber,
            a.AppointmentDate,
            a.Status.ToString(),
            a.Service?.Name,
            a.Symptoms,
            false,
            a.FollowUpFromAppointmentId != null
        )).ToList();
    }
}
