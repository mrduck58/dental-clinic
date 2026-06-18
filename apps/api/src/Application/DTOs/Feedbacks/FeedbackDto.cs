namespace DentalClinic.API.Application.DTOs.Feedbacks;

public record FeedbackDto(
    Guid Id,
    string CustomerName,
    int Rating,
    string Comment,
    string Status,
    string? ReplyText,
    DateTimeOffset? RepliedAt,
    DateTimeOffset CreatedAt);

public record CreateFeedbackRequest(
    string CustomerName,
    int Rating,
    string Comment);

public record ReplyFeedbackRequest(
    string ReplyText);
