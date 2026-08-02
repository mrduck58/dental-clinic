using DentalClinic.API.Application.DTOs.StaffDashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.StaffDashboard;

public record GetStaffDashboardStatsQuery : IRequest<StaffDashboardStatsDto>;

/// <summary>Đọc trực tiếp từ AppDbContext — truy vấn báo cáo/tổng hợp đa entity, cùng phong cách
/// với các Dashboard handler khác.</summary>
public class GetStaffDashboardStatsHandler(AppDbContext dbContext)
    : IRequestHandler<GetStaffDashboardStatsQuery, StaffDashboardStatsDto>
{
    public async Task<StaffDashboardStatsDto> Handle(GetStaffDashboardStatsQuery query, CancellationToken ct)
    {
        var (start, end) = StaffDashboardDateHelper.TodayVnRange();

        var appointmentsTodayCount = await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status != AppointmentStatus.Cancelled, ct);

        var waitingCheckInCount = await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status == AppointmentStatus.Confirmed, ct);

        var inProgressCount = await dbContext.Appointments.CountAsync(
            a => a.AppointmentDate >= start && a.AppointmentDate < end && a.Status == AppointmentStatus.InProgress, ct);

        var pendingInvoicesCount = await dbContext.Invoices.CountAsync(i => i.Status == PaymentStatus.Unpaid, ct);

        return new StaffDashboardStatsDto(appointmentsTodayCount, waitingCheckInCount, inProgressCount, pendingInvoicesCount);
    }
}
