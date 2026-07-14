namespace DentalClinic.API.Domain.Interfaces.Services;

/// <summary>
/// Gợi ý đặt lịch được AI trích xuất từ hội thoại (tên dịch vụ/bác sĩ/người thân ở dạng tự nhiên,
/// chưa đối chiếu với DB — việc đối chiếu ra Id do <c>SendChatMessageHandler</c> đảm nhiệm).
/// <c>ConfirmBooking</c> = true nghĩa là bệnh nhân đã xác nhận đặt lịch trong hội thoại và AI đã
/// gom đủ bác sĩ + ngày + giờ — handler sẽ thực sự tạo lịch hẹn thay vì chỉ gợi ý.
/// <c>ConfirmCancel</c> = true nghĩa là bệnh nhân đã xác nhận muốn hủy một lịch hẹn cụ thể, xác định
/// qua <c>CancelAppointmentCodeHint</c> (mã lịch hẹn AI đã đọc lại đúng từ dữ liệu được cung cấp).
/// <c>ConfirmReschedule</c> = true nghĩa là bệnh nhân đã xác nhận dời một lịch hẹn cụ thể (xác định
/// qua <c>RescheduleAppointmentCodeHint</c>) sang ngày/giờ mới (dùng chung <c>PreferredDate</c>/
/// <c>PreferredTime</c> ở trên — cùng bác sĩ với lịch hẹn gốc, không đổi bác sĩ).
/// </summary>
public record AiChatReply(
    string Reply,
    bool SuggestBooking,
    string? ServiceNameHint = null,
    string? DentistNameHint = null,
    DateOnly? PreferredDate = null,
    string? NotesHint = null,
    bool ConfirmBooking = false,
    TimeOnly? PreferredTime = null,
    string? PatientNameHint = null,
    bool ConfirmCancel = false,
    string? CancelAppointmentCodeHint = null,
    bool ConfirmReschedule = false,
    string? RescheduleAppointmentCodeHint = null);

/// <summary>Trừu tượng hóa nhà cung cấp AI (hiện tại: Gemini) cho chatbot tư vấn thông tin phòng khám.</summary>
public interface IAiChatService
{
    Task<AiChatReply> AskAsync(string systemInstruction, string userMessage, CancellationToken ct = default);

    /// <summary>Sinh văn bản thuần (không theo cấu trúc JSON) — dùng cho các tác vụ tổng hợp/tóm tắt/soạn
    /// nội dung nội bộ. <paramref name="feature"/> là tên tính năng gọi (vd. "PatientSummary",
    /// "MarketingContent") — chỉ dùng để ghi nhận usage log, không ảnh hưởng nội dung gọi AI.</summary>
    Task<string> SummarizeAsync(
        string systemInstruction, string content, string feature = "Summarize", CancellationToken ct = default);
}
