using System.Text.Json;
using System.Text.Json.Serialization;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Tích hợp Google Gemini (model mặc định: gemini-3.1-flash-lite) cho chatbot tư vấn thông tin phòng khám.
/// Dùng chế độ JSON output của Gemini (ResponseMimeType = "application/json") để bot luôn trả về đúng
/// cấu trúc { reply, suggestBooking, bookingHint } thay vì phải so khớp từ khóa trên câu trả lời dạng
/// văn bản tự do. bookingHint chỉ chứa tên dịch vụ/bác sĩ ở dạng tự nhiên do AI trích xuất — việc đối
/// chiếu sang Id thật trong DB do SendChatMessageHandler đảm nhiệm.
/// </summary>
public class GeminiChatService(
    IOptions<GeminiSettings> options,
    ILogger<GeminiChatService> logger) : IAiChatService
{
    private readonly GeminiSettings _settings = options.Value;

    public async Task<AiChatReply> AskAsync(string systemInstruction, string userMessage, CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new ValidationException("Chatbot AI chưa được cấu hình (thiếu Gemini API key).");
        }

        var client = new Client(apiKey: _settings.ApiKey);
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemInstruction }],
            },
            ResponseMimeType = "application/json",
        };

        var response = await client.Models.GenerateContentAsync(
            model: _settings.Model,
            contents: userMessage,
            config: config);

        var rawText = response.Text;
        if (string.IsNullOrWhiteSpace(rawText))
        {
            logger.LogWarning("Gemini trả về nội dung rỗng cho câu hỏi: {Message}", userMessage);
            return new AiChatReply("Xin lỗi, hiện tại tôi chưa thể trả lời câu hỏi này. Vui lòng thử lại sau.", false);
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<GeminiJsonReply>(rawText);
            if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Reply))
            {
                DateOnly? preferredDate = null;
                if (parsed.BookingHint?.PreferredDate is { } dateText &&
                    DateOnly.TryParse(dateText, out var parsedDate))
                {
                    preferredDate = parsedDate;
                }

                return new AiChatReply(
                    parsed.Reply,
                    parsed.SuggestBooking,
                    ServiceNameHint: parsed.BookingHint?.ServiceName,
                    DentistNameHint: parsed.BookingHint?.DentistName,
                    PreferredDate: preferredDate,
                    NotesHint: parsed.BookingHint?.Notes);
            }
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Không parse được JSON từ Gemini, trả nguyên văn. Raw: {Raw}", rawText);
        }

        return new AiChatReply(rawText, false);
    }

    public async Task<string> SummarizeAsync(string systemInstruction, string content, CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            throw new ValidationException("Chức năng AI chưa được cấu hình (thiếu Gemini API key).");
        }

        var client = new Client(apiKey: _settings.ApiKey);
        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemInstruction }],
            },
        };

        var response = await client.Models.GenerateContentAsync(
            model: _settings.Model,
            contents: content,
            config: config);

        var text = response.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("Gemini trả về nội dung rỗng khi tóm tắt.");
            return "Không thể tạo tóm tắt lúc này. Vui lòng thử lại sau.";
        }

        return text.Trim();
    }

    private sealed record GeminiJsonReply(
        [property: JsonPropertyName("reply")] string Reply,
        [property: JsonPropertyName("suggestBooking")] bool SuggestBooking,
        [property: JsonPropertyName("bookingHint")] GeminiJsonBookingHint? BookingHint);

    private sealed record GeminiJsonBookingHint(
        [property: JsonPropertyName("serviceName")] string? ServiceName,
        [property: JsonPropertyName("dentistName")] string? DentistName,
        [property: JsonPropertyName("preferredDate")] string? PreferredDate,
        [property: JsonPropertyName("notes")] string? Notes);
}
