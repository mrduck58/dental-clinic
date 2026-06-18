using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public class GetFeedbacksHandler(IFeedbackRepository feedbackRepository)
{
    public async Task<IEnumerable<FeedbackDto>> HandleAsync(
        string? status,
        string? search,
        CancellationToken ct = default)
    {
        var feedbacks = await feedbackRepository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(status))
            feedbacks = feedbacks.Where(f => f.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
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
