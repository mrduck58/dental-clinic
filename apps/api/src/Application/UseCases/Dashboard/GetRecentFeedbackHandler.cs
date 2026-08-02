using DentalClinic.API.Application.DTOs.Dashboard;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Dashboard;

public record GetRecentFeedbackQuery(int Limit) : IRequest<RecentFeedbackDto>;

public class GetRecentFeedbackHandler(AppDbContext dbContext) : IRequestHandler<GetRecentFeedbackQuery, RecentFeedbackDto>
{
    public async Task<RecentFeedbackDto> Handle(GetRecentFeedbackQuery query, CancellationToken ct)
    {
        var clampedLimit = Math.Clamp(query.Limit, 1, 20);

        var featuredQuery = dbContext.Feedbacks.AsNoTracking().Where(f => f.Status == FeedbackStatus.Featured);

        var totalFeatured = await featuredQuery.CountAsync(ct);
        var averageRating = totalFeatured > 0
            ? Math.Round(await featuredQuery.AverageAsync(f => (double)f.Rating, ct), 1)
            : 0;

        var items = await featuredQuery
            .OrderByDescending(f => f.CreatedAt)
            .Take(clampedLimit)
            .Select(f => new FeedbackSummaryDto(f.Id, f.CustomerName, f.Rating, f.Comment, f.CreatedAt))
            .ToListAsync(ct);

        return new RecentFeedbackDto(items, averageRating, totalFeatured);
    }
}
