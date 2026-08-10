namespace DentalClinic.API.Domain.Entities;

/// <summary>Một phiên hội thoại giữa bệnh nhân và chatbot AI tư vấn thông tin phòng khám.</summary>
public class ChatConversation
{
    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<ChatMessage> Messages { get; private set; } = new List<ChatMessage>();
    public Patient Patient { get; private set; } = null!;

    private ChatConversation() { }

    public static ChatConversation Create(Guid patientId)
    {
        var now = DateTimeOffset.UtcNow;
        return new ChatConversation
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
