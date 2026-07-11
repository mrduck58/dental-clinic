namespace DentalClinic.API.Domain.Entities;

/// <summary>Một tin nhắn trong hội thoại chatbot — <see cref="Role"/> là "user" hoặc "assistant".</summary>
public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public ChatConversation Conversation { get; private set; } = null!;

    private ChatMessage() { }

    public static ChatMessage Create(Guid conversationId, string role, string content) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId,
        Role = role,
        Content = content,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
