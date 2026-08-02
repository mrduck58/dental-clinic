using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static DentalClinic.API.Application.UseCases.Dashboard.DashboardDateHelper;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetDashboardTodayAppointmentsQuery(int Page, int PageSize) : IRequest<TodayAppointmentsDto>;

public class GetDashboardTodayAppointmentsHandler(AppDbContext dbContext)
    : IRequestHandler<GetDashboardTodayAppointmentsQuery, TodayAppointmentsDto>
{
    public async Task<TodayAppointmentsDto> Handle(GetDashboardTodayAppointmentsQuery query, CancellationToken ct)
    {
        var clampedPage = Math.Max(query.Page, 1);
        var clampedPageSize = Math.Clamp(query.PageSize, 1, 100);

        var today = GetVietnamToday();
        var startOffset = ToVn(today);
        var endOffset = ToVn(today.AddDays(1));

        var appointmentsQuery = dbContext.Appointments
            .AsNoTracking()
            .Where(a => a.AppointmentDate >= startOffset && a.AppointmentDate < endOffset)
            .OrderBy(a => a.AppointmentDate);

        var total = await appointmentsQuery.CountAsync(ct);
        var items = await appointmentsQuery
            .Skip((clampedPage - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .Select(a => new TodayAppointmentItemDto(
                a.Id,
                a.AppointmentDate,
                a.Patient.User.FullName ?? string.Empty,
                a.Service != null ? a.Service.Name : null,
                a.Status.ToString()))
            .ToListAsync(ct);

        return new TodayAppointmentsDto(
            items, total, clampedPage, clampedPageSize, (int)Math.Ceiling((double)total / clampedPageSize));
    }
}
