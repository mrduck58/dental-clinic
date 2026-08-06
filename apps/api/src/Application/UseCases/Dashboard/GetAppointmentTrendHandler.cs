using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetAppointmentTrendQuery(string? Range) : IRequest<AppointmentTrendDto>;

public class GetAppointmentTrendHandler(IDashboardQueryService dashboardQueryService)
    : IRequestHandler<GetAppointmentTrendQuery, AppointmentTrendDto>
{
    public Task<AppointmentTrendDto> Handle(GetAppointmentTrendQuery query, CancellationToken ct) =>
        dashboardQueryService.GetAppointmentTrendAsync(query.Range, ct);
}
