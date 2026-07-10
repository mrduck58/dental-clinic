namespace DentalClinic.API.Domain.Interfaces.Services;

/// <summary>
/// Gợi ý đặt lịch được AI trích xuất từ hội thoại (tên dịch vụ/bác sĩ ở dạng tự nhiên,
/// chưa đối chiếu với DB — việc đối chiếu ra Id do <c>SendChatMessageHandler</c> đảm nhiệm).
/// </summary>
public record AiChatReply(
    string Reply,
    bool SuggestBooking,
    string? ServiceNameHint = null,
    string? DentistNameHint = null,
    DateOnly? PreferredDate = null,
    string? NotesHint = null);

/// <summary>Trừu tượng hóa nhà cung cấp AI (hiện tại: Gemini) cho chatbot tư vấn thông tin phòng khám.</summary>
public interface IAiChatService
{
    Task<AiChatReply> AskAsync(string systemInstruction, string userMessage, CancellationToken ct = default);

    /// <summary>Sinh văn bản thuần (không theo cấu trúc JSON) — dùng cho các tác vụ tổng hợp/tóm tắt nội bộ.</summary>
    Task<string> SummarizeAsync(string systemInstruction, string content, CancellationToken ct = default);
}
