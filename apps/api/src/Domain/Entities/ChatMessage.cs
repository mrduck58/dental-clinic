namespace DentalClinic.API.Domain.Entities;

/// <summary>Một tin nhắn trong hội thoại chatbot — <see cref="Role"/> là "user" hoặc "assistant".
/// <see cref="SuggestBooking"/>/<see cref="BookingActionTaken"/> chỉ có ý nghĩa với tin nhắn assistant —
/// lưu lại (thay vì suy luận ngược từ nội dung văn bản) để có số liệu thống kê đáng tin cậy về hiệu quả
/// chatbot (tỷ lệ gợi ý đặt lịch, tỷ lệ đặt/hủy lịch thành công qua chat).</summary>
public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public bool SuggestBooking { get; private set; }
    public bool BookingActionTaken { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public ChatConversation Conversation { get; private set; } = null!;

    private ChatMessage() { }

    public static ChatMessage Create(
        Guid conversationId, string role, string content,
        bool suggestBooking = false, bool bookingActionTaken = false) => new()
    {
        Id = Guid.NewGuid(),
        ConversationId = conversationId,
        Role = role,
        Content = content,
        SuggestBooking = suggestBooking,
        BookingActionTaken = bookingActionTaken,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
