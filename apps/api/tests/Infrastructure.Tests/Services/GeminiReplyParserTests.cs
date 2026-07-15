using DentalClinic.API.Infrastructure.Services;
using FluentAssertions;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Services;

[TestFixture]
public class GeminiReplyParserTests
{
    /// <summary>JSON hợp lệ đầy đủ trường phải parse đúng từng trường, bao gồm cả các trường mới
    /// (confirmCancel/cancelAppointmentCode/patientName) không có trong phiên bản đầu tiên.</summary>
    [Test]
    public void Parse_FullValidJson_MapsAllFields()
    {
        const string json = """
            {"reply": "Đã đặt lịch cho bạn", "suggestBooking": false, "confirmBooking": true,
             "confirmCancel": false, "cancelAppointmentCode": null,
             "bookingHint": {"serviceName": "Trám răng", "dentistName": "BS A", "preferredDate": "2026-08-01",
             "preferredTime": "09:00", "notes": "đau răng hàm", "patientName": "con tôi"}}
            """;

        var result = GeminiReplyParser.Parse(json);

        result.Reply.Should().Be("Đã đặt lịch cho bạn");
        result.SuggestBooking.Should().BeFalse();
        result.ConfirmBooking.Should().BeTrue();
        result.ConfirmCancel.Should().BeFalse();
        result.ServiceNameHint.Should().Be("Trám răng");
        result.DentistNameHint.Should().Be("BS A");
        result.PreferredDate.Should().Be(new DateOnly(2026, 8, 1));
        result.PreferredTime.Should().Be(new TimeOnly(9, 0));
        result.NotesHint.Should().Be("đau răng hàm");
        result.PatientNameHint.Should().Be("con tôi");
    }

    /// <summary>Luồng hủy lịch: confirmCancel = true kèm mã lịch hẹn phải được đọc đúng.</summary>
    [Test]
    public void Parse_CancelConfirmation_MapsCancelFields()
    {
        const string json = """
            {"reply": "Đã hủy lịch giúp bạn", "suggestBooking": false, "confirmBooking": false,
             "confirmCancel": true, "cancelAppointmentCode": "DK20260801ABC123",
             "bookingHint": {"serviceName": null, "dentistName": null, "preferredDate": null,
             "preferredTime": null, "notes": null, "patientName": null}}
            """;

        var result = GeminiReplyParser.Parse(json);

        result.ConfirmCancel.Should().BeTrue();
        result.CancelAppointmentCodeHint.Should().Be("DK20260801ABC123");
    }

    /// <summary>Luồng dời lịch: confirmReschedule = true kèm mã lịch hẹn cũ và ngày/giờ mới (trong
    /// bookingHint) phải được đọc đúng.</summary>
    [Test]
    public void Parse_RescheduleConfirmation_MapsRescheduleFields()
    {
        const string json = """
            {"reply": "Đã dời lịch giúp bạn", "suggestBooking": false, "confirmBooking": false,
             "confirmCancel": false, "confirmReschedule": true, "rescheduleAppointmentCode": "DK20260801ABC123",
             "bookingHint": {"serviceName": null, "dentistName": null, "preferredDate": "2026-08-05",
             "preferredTime": "14:00", "notes": null, "patientName": null}}
            """;

        var result = GeminiReplyParser.Parse(json);

        result.ConfirmReschedule.Should().BeTrue();
        result.RescheduleAppointmentCodeHint.Should().Be("DK20260801ABC123");
        result.PreferredDate.Should().Be(new DateOnly(2026, 8, 5));
        result.PreferredTime.Should().Be(new TimeOnly(14, 0));
    }

    /// <summary>JSON thiếu các trường mới (dữ liệu cũ/tương thích ngược) vẫn phải parse được, với các
    /// trường mới mặc định về false/null thay vì ném lỗi.</summary>
    [Test]
    public void Parse_JsonMissingNewFields_DefaultsGracefully()
    {
        const string json = """{"reply": "Chào bạn", "suggestBooking": false}""";

        var result = GeminiReplyParser.Parse(json);

        result.Reply.Should().Be("Chào bạn");
        result.ConfirmBooking.Should().BeFalse();
        result.ConfirmCancel.Should().BeFalse();
        result.CancelAppointmentCodeHint.Should().BeNull();
        result.ConfirmReschedule.Should().BeFalse();
        result.RescheduleAppointmentCodeHint.Should().BeNull();
        result.PatientNameHint.Should().BeNull();
    }

    /// <summary>JSON hỏng cú pháp (Gemini lỡ trả kèm markdown hay bị cắt cụt) không được ném exception ra
    /// ngoài — phải rơi về trả nguyên văn raw text làm reply, suggestBooking = false.</summary>
    [Test]
    public void Parse_MalformedJson_FallsBackToRawTextReply()
    {
        const string malformed = "```json\n{\"reply\": \"thiếu dấu ngoặc\"";

        var result = GeminiReplyParser.Parse(malformed);

        result.Reply.Should().Be(malformed);
        result.SuggestBooking.Should().BeFalse();
        result.ConfirmBooking.Should().BeFalse();
    }

    /// <summary>JSON hợp lệ nhưng "reply" rỗng/trắng phải được coi như không hợp lệ — cũng rơi về
    /// raw text, tránh trả về câu trả lời trống cho bệnh nhân.</summary>
    [Test]
    public void Parse_ValidJsonButBlankReply_FallsBackToRawText()
    {
        const string json = """{"reply": "   ", "suggestBooking": false}""";

        var result = GeminiReplyParser.Parse(json);

        result.Reply.Should().Be(json);
    }

    /// <summary>Ngày/giờ sai định dạng (AI trả khác "yyyy-MM-dd"/"HH:mm") phải bị bỏ qua (null)
    /// thay vì ném lỗi làm hỏng toàn bộ phản hồi.</summary>
    [Test]
    public void Parse_UnparsablePreferredDateAndTime_SetsNullInstead()
    {
        const string json = """
            {"reply": "ok", "suggestBooking": false, "confirmBooking": false,
             "bookingHint": {"preferredDate": "ngày mai", "preferredTime": "chiều"}}
            """;

        var result = GeminiReplyParser.Parse(json);

        result.PreferredDate.Should().BeNull();
        result.PreferredTime.Should().BeNull();
    }
}
