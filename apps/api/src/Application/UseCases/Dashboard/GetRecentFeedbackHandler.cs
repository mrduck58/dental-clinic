using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Application.Interfaces;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetRecentFeedbackQuery(int Limit) : IRequest<RecentFeedbackDto>;

public class GetRecentFeedbackHandler(IDashboardQueryService dashboardQueryService)
    : IRequestHandler<GetRecentFeedbackQuery, RecentFeedbackDto>
{
    public Task<RecentFeedbackDto> Handle(GetRecentFeedbackQuery query, CancellationToken ct) =>
        dashboardQueryService.GetRecentFeedbackAsync(query.Limit, ct);
}
