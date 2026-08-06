using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetDashboardTodayAppointmentsQuery(int Page, int PageSize) : IRequest<TodayAppointmentsDto>;

public class GetDashboardTodayAppointmentsHandler(IDashboardQueryService dashboardQueryService)
    : IRequestHandler<GetDashboardTodayAppointmentsQuery, TodayAppointmentsDto>
{
    public Task<TodayAppointmentsDto> Handle(GetDashboardTodayAppointmentsQuery query, CancellationToken ct) =>
        dashboardQueryService.GetTodayAppointmentsAsync(query.Page, query.PageSize, ct);
}
