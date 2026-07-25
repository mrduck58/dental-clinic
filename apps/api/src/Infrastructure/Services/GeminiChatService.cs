using System.Diagnostics;
using System.Net;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Settings;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Tích hợp Google Gemini (model mặc định: gemini-3.1-flash-lite) cho các tính năng AI của phòng khám
/// (chatbot tư vấn, tóm tắt bệnh án, soạn nội dung marketing). Dùng chế độ JSON output của Gemini
/// (ResponseMimeType = "application/json") cho chatbot để bot luôn trả về đúng cấu trúc thay vì phải so
/// khớp từ khóa trên câu trả lời dạng văn bản tự do — việc parse JSON nằm ở <see cref="GeminiReplyParser"/>.
/// Mỗi lệnh gọi đều được: (1) thử lại 1 lần nếu lỗi tạm thời (429/503...), (2) ghi lại vào
/// <see cref="AiUsageLog"/> (feature/thời gian/thành công hay không) để theo dõi chi phí — xem
/// <see cref="LogUsageAsync"/>. Lỗi ghi log không được làm hỏng phản hồi chính.
/// </summary>
public class GeminiChatService(
    IOptions<GeminiSettings> options,
    AppDbContext dbContext,
    ILogger<GeminiChatService> logger) : IAiChatService
{
    private const string ChatFeature = "ChatBot";
    private const int MaxRetries = 1;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    private readonly GeminiSettings _settings = options.Value;

    public async Task<AiChatReply> AskAsync(string systemInstruction, string userMessage, CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new ValidationException("Chatbot AI chưa được cấu hình (thiếu Gemini API key).");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var rawText = await CallGeminiWithRetryAsync(systemInstruction, userMessage, jsonMode: true, ct);
            await LogUsageAsync(ChatFeature, success: true, stopwatch.Elapsed, error: null, ct);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                logger.LogWarning("Gemini trả về nội dung rỗng cho câu hỏi: {Message}", userMessage);
                return new AiChatReply(
                    "Xin lỗi, hiện tại tôi chưa thể trả lời câu hỏi này. Vui lòng thử lại sau.", false);
            }

            return GeminiReplyParser.Parse(rawText, logger);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await LogUsageAsync(ChatFeature, success: false, stopwatch.Elapsed, ex.Message, ct);
            throw;
        }
    }

    public async Task<string> SummarizeAsync(
        string systemInstruction, string content, string feature = "Summarize", CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new ValidationException("Chức năng AI chưa được cấu hình (thiếu Gemini API key).");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var text = await CallGeminiWithRetryAsync(systemInstruction, content, jsonMode: false, ct);
            await LogUsageAsync(feature, success: true, stopwatch.Elapsed, error: null, ct);

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("Gemini trả về nội dung rỗng cho tính năng {Feature}.", feature);
                return "Không thể tạo nội dung lúc này. Vui lòng thử lại sau.";
            }

            return text.Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await LogUsageAsync(feature, success: false, stopwatch.Elapsed, ex.Message, ct);
            throw;
        }
    }

    /// <summary>Gọi Gemini, thử lại đúng 1 lần nếu lỗi có vẻ tạm thời (429/5xx) — quota lỗi cứng hay API
    /// key sai sẽ không được retry vì <see cref="IsTransient"/> trả false ngay từ lần đầu.</summary>
    private async Task<string?> CallGeminiWithRetryAsync(
        string systemInstruction, string userContent, bool jsonMode, CancellationToken ct)
    {
        var client = new Client(apiKey: _settings.ApiKey);
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemInstruction }],
            },
            ResponseMimeType = jsonMode ? "application/json" : null,
        };

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var response = await client.Models.GenerateContentAsync(
                    model: _settings.Model, contents: userContent, config: config);
                return response.Text;
            }
            catch (Exception ex) when (attempt < MaxRetries && IsTransient(ex))
            {
                logger.LogWarning(ex, "Gemini lỗi tạm thời (lần {Attempt}), thử lại sau {DelayMs}ms.",
                    attempt + 1, RetryDelay.TotalMilliseconds);
                await Task.Delay(RetryDelay, ct);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        if (ex is HttpRequestException httpEx && httpEx.StatusCode is
            HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout)
        {
            return true;
        }

        // Google.GenAI SDK có thể bọc lỗi HTTP thành exception riêng không kèm StatusCode —
        // dự phòng bằng cách nhận diện qua nội dung message.
        var message = ex.Message;
        return message.Contains("429") || message.Contains("503") ||
            message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("UNAVAILABLE", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Ghi nhận một lệnh gọi Gemini để theo dõi chi phí/hiệu năng theo tính năng. Không lưu
    /// prompt/phản hồi (tránh phình dữ liệu, tránh lộ thông tin bệnh nhân). Lỗi ghi log chỉ log cảnh báo,
    /// không được làm hỏng phản hồi chính cho người dùng.</summary>
    private async Task LogUsageAsync(
        string feature, bool success, TimeSpan duration, string? error, CancellationToken ct)
    {
        try
        {
            var trimmedError = error is { Length: > 500 } ? error[..500] : error;
            dbContext.AiUsageLogs.Add(
                AiUsageLog.Create(feature, success, (int)duration.TotalMilliseconds, trimmedError));
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception logEx)
        {
            logger.LogWarning(logEx, "Không ghi được AiUsageLog cho tính năng {Feature}.", feature);
        }
    }
}
