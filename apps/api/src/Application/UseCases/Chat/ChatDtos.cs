namespace DentalClinic.API.Application.UseCases.Chat;

/// <summary>InitialMessage khác null khi bệnh nhân có lịch hẹn sắp tới — bot chủ động nhắc ngay khi
/// bắt đầu một cuộc trò chuyện MỚI, thay vì chỉ bị động chờ được hỏi.</summary>
public record StartConversationResult(Guid ConversationId, string? InitialMessage = null);

/// <summary>
/// BookingCreated/BookingCancelled/BookingRescheduled = true khi bot đã thực sự đặt/hủy/dời lịch hẹn
/// ngay trong hội thoại — mobile hiển thị nút "Xem lịch hẹn" (kèm AppointmentCode) thay vì nút "Đặt
/// lịch". Ba cờ này loại trừ nhau trong cùng một tin nhắn.
/// </summary>
public record SendChatMessageResult(
    string Reply,
    bool SuggestBooking,
    BookingHintDto BookingHint,
    bool BookingCreated = false,
    string? AppointmentCode = null,
    bool BookingCancelled = false,
    bool BookingRescheduled = false);

/// <summary>
/// Gợi ý điền sẵn cho luồng đặt lịch, rút ra từ hội thoại. ServiceId/DentistId/PatientId chỉ khác null
/// khi SendChatMessageHandler đối chiếu được tên AI trích xuất với dữ liệu thật trong DB — mobile chỉ
/// nên dùng các trường đã có Id, không dùng tên "trôi nổi" chưa đối chiếu được để tránh điền sai dữ liệu.
/// PatientId/PatientName là người được đặt lịch (chính chủ hoặc người thân), null nếu bot mặc định
/// đặt cho chính chủ tài khoản.
/// </summary>
public record BookingHintDto(
    Guid? ServiceId,
    string? ServiceName,
    Guid? DentistId,
    string? DentistName,
    DateOnly? PreferredDate,
    string? Notes,
    Guid? PatientId = null,
    string? PatientName = null);

public record ChatConversationSummaryDto(Guid Id, string Preview, DateTimeOffset UpdatedAt);

public record ChatMessageDto(string Role, string Content, DateTimeOffset CreatedAt);

public record ConversationMessagesDto(Guid ConversationId, IReadOnlyList<ChatMessageDto> Messages);
