namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một lần gọi Gemini — ghi lại để theo dõi chi phí/hiệu năng/tỷ lệ lỗi của từng tính năng AI
/// (chatbot, tóm tắt bệnh án, soạn nội dung marketing...) qua thời gian. Không lưu nội dung prompt/
/// phản hồi (tránh phình dữ liệu và rủi ro lộ thông tin bệnh nhân) — chỉ số liệu vận hành.
/// </summary>
public class AiUsageLog
{
    public Guid Id { get; private set; }

    /// <summary>Tên tính năng gọi AI, ví dụ "ChatBot", "PatientSummary", "MarketingContent".</summary>
    public string Feature { get; private set; } = string.Empty;

    public bool Success { get; private set; }
    public int DurationMs { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    private AiUsageLog() { }

    public static AiUsageLog Create(string feature, bool success, int durationMs, string? errorMessage) => new()
    {
        Id = Guid.NewGuid(),
        Feature = feature,
        Success = success,
        DurationMs = durationMs,
        ErrorMessage = errorMessage,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}
