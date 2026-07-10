namespace DentalClinic.API.Application.UseCases.Chat;

public record StartConversationResult(Guid ConversationId);

public record SendChatMessageResult(string Reply, bool SuggestBooking, BookingHintDto BookingHint);

/// <summary>
/// Gợi ý điền sẵn cho luồng đặt lịch, rút ra từ hội thoại. ServiceId/DentistId chỉ khác null khi
/// SendChatMessageHandler đối chiếu được tên AI trích xuất với dữ liệu thật trong DB — mobile chỉ nên
/// dùng các trường đã có Id, không dùng tên "trôi nổi" chưa đối chiếu được để tránh điền sai dữ liệu.
/// </summary>
public record BookingHintDto(
    Guid? ServiceId,
    string? ServiceName,
    Guid? DentistId,
    string? DentistName,
    DateOnly? PreferredDate,
    string? Notes);

public record ChatConversationSummaryDto(Guid Id, string Preview, DateTimeOffset UpdatedAt);

public record ChatMessageDto(string Role, string Content, DateTimeOffset CreatedAt);

public record ConversationMessagesDto(Guid ConversationId, IReadOnlyList<ChatMessageDto> Messages);
