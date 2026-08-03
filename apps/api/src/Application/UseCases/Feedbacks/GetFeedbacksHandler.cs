using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public record GetFeedbacksQuery(string? Status, string? Search) : IRequest<IEnumerable<FeedbackDto>>;

public class GetFeedbacksHandler(IFeedbackRepository feedbackRepository)
    : IRequestHandler<GetFeedbacksQuery, IEnumerable<FeedbackDto>>
{
    public async Task<IEnumerable<FeedbackDto>> Handle(GetFeedbacksQuery request, CancellationToken cancellationToken)
    {
        var feedbacks = await feedbackRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Status))
            feedbacks = feedbacks.Where(f => f.Status.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.ToLower();
            feedbacks = feedbacks.Where(f =>
                f.CustomerName.ToLower().Contains(q) ||
                f.Comment.ToLower().Contains(q));
        }

        return feedbacks.Select(ToDto);
    }

    internal static FeedbackDto ToDto(Feedback f) => new(
        f.Id,
        f.CustomerName,
        f.Rating,
        f.Comment,
        f.Status.ToString(),
        f.ReplyText,
        f.RepliedAt,
        f.CreatedAt);
}
