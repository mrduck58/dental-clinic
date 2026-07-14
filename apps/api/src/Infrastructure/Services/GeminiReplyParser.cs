using System.Text.Json;
using System.Text.Json.Serialization;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Parse phần JSON dạng { reply, suggestBooking, confirmBooking, confirmCancel, cancelAppointmentCode,
/// confirmReschedule, rescheduleAppointmentCode, bookingHint } mà Gemini trả về (chế độ
/// ResponseMimeType = "application/json") thành <see cref="AiChatReply"/>.
/// Tách riêng khỏi <see cref="GeminiChatService"/> để test được logic parse (bao gồm cả trường hợp JSON lỗi/thiếu
/// trường) mà không cần gọi API Gemini thật.
/// </summary>
public static class GeminiReplyParser
{
    public static AiChatReply Parse(string rawText, ILogger? logger = null)
    {
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

                TimeOnly? preferredTime = null;
                if (parsed.BookingHint?.PreferredTime is { } timeText &&
                    TimeOnly.TryParse(timeText, out var parsedTime))
                {
                    preferredTime = parsedTime;
                }

                return new AiChatReply(
                    parsed.Reply,
                    parsed.SuggestBooking,
                    ServiceNameHint: parsed.BookingHint?.ServiceName,
                    DentistNameHint: parsed.BookingHint?.DentistName,
                    PreferredDate: preferredDate,
                    NotesHint: parsed.BookingHint?.Notes,
                    ConfirmBooking: parsed.ConfirmBooking,
                    PreferredTime: preferredTime,
                    PatientNameHint: parsed.BookingHint?.PatientName,
                    ConfirmCancel: parsed.ConfirmCancel,
                    CancelAppointmentCodeHint: parsed.CancelAppointmentCode,
                    ConfirmReschedule: parsed.ConfirmReschedule,
                    RescheduleAppointmentCodeHint: parsed.RescheduleAppointmentCode);
            }
        }
        catch (JsonException ex)
        {
            logger?.LogWarning(ex, "Không parse được JSON từ Gemini, trả nguyên văn. Raw: {Raw}", rawText);
        }

        return new AiChatReply(rawText, false);
    }

    private sealed record GeminiJsonReply(
        [property: JsonPropertyName("reply")] string Reply,
        [property: JsonPropertyName("suggestBooking")] bool SuggestBooking,
        [property: JsonPropertyName("confirmBooking")] bool ConfirmBooking,
        [property: JsonPropertyName("confirmCancel")] bool ConfirmCancel,
        [property: JsonPropertyName("cancelAppointmentCode")] string? CancelAppointmentCode,
        [property: JsonPropertyName("confirmReschedule")] bool ConfirmReschedule,
        [property: JsonPropertyName("rescheduleAppointmentCode")] string? RescheduleAppointmentCode,
        [property: JsonPropertyName("bookingHint")] GeminiJsonBookingHint? BookingHint);

    private sealed record GeminiJsonBookingHint(
        [property: JsonPropertyName("serviceName")] string? ServiceName,
        [property: JsonPropertyName("dentistName")] string? DentistName,
        [property: JsonPropertyName("preferredDate")] string? PreferredDate,
        [property: JsonPropertyName("preferredTime")] string? PreferredTime,
        [property: JsonPropertyName("notes")] string? Notes,
        [property: JsonPropertyName("patientName")] string? PatientName);
}
