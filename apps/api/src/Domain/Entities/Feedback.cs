using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

public class Feedback
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public int Rating { get; private set; }
    public string Comment { get; private set; } = string.Empty;
    public FeedbackStatus Status { get; private set; }
    public string? ReplyText { get; private set; }
    public DateTimeOffset? RepliedAt { get; private set; }
    public Guid? PatientId { get; private set; }
    public Patient? Patient { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Feedback() { }

    public static Feedback Create(string customerName, int rating, string comment, Guid? patientId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            Rating = rating,
            Comment = comment,
            PatientId = patientId,
            Status = FeedbackStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Feature()
    {
        Status = FeedbackStatus.Featured;
    }

    public void Unfeature()
    {
        Status = FeedbackStatus.Pending;
    }

    public void Hide()
    {
        Status = FeedbackStatus.Hidden;
    }

    public void Reply(string replyText)
    {
        ReplyText = replyText;
        RepliedAt = DateTimeOffset.UtcNow;
        if (Status != FeedbackStatus.Hidden)
            Status = FeedbackStatus.Featured;
    }
}
