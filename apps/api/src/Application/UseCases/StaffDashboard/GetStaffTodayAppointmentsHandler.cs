using DentalClinic.API.Application.DTOs.StaffDashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.StaffDashboard;

public record GetStaffTodayAppointmentsQuery(int Limit) : IRequest<IReadOnlyList<StaffTodayAppointmentDto>>;

/// <summary>Lịch hẹn hôm nay đang ở trạng thái cần staff theo dõi (đã xác nhận / đã check-in / đang khám).</summary>
public class GetStaffTodayAppointmentsHandler(AppDbContext dbContext)
    : IRequestHandler<GetStaffTodayAppointmentsQuery, IReadOnlyList<StaffTodayAppointmentDto>>
{
    private static readonly AppointmentStatus[] ActiveTodayStatuses =
    [
        AppointmentStatus.Confirmed,
        AppointmentStatus.CheckedIn,
        AppointmentStatus.InProgress
    ];

    public async Task<IReadOnlyList<StaffTodayAppointmentDto>> Handle(GetStaffTodayAppointmentsQuery query, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(query.Limit, 1, 50);
        var (start, end) = StaffDashboardDateHelper.TodayVnRange();

        return await dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate < end
                        && ActiveTodayStatuses.Contains(a.Status))
            .OrderBy(a => a.AppointmentDate)
            .Take(clampedLimit)
            .Select(a => new StaffTodayAppointmentDto(
                a.Id,
                a.Patient.User.FullName ?? string.Empty,
                a.Service != null ? a.Service.Name : null,
                a.Dentist.Employee.User.FullName ?? string.Empty,
                a.AppointmentDate,
                a.Status.ToString()))
            .ToListAsync(ct);
    }
}
