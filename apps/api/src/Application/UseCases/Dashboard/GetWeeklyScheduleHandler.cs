using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetWeeklyScheduleQuery(DateOnly? Date) : IRequest<WeeklyScheduleDto>;

public class GetWeeklyScheduleHandler(IDashboardQueryService dashboardQueryService)
    : IRequestHandler<GetWeeklyScheduleQuery, WeeklyScheduleDto>
{
    public Task<WeeklyScheduleDto> Handle(GetWeeklyScheduleQuery query, CancellationToken ct) =>
        dashboardQueryService.GetWeeklyScheduleAsync(query.Date, ct);
}
