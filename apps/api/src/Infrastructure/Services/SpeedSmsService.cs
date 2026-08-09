using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Gửi SMS qua SpeedSMS (nhà cung cấp trong nước). Chọn SpeedSMS thay vì Twilio vì gửi về số Việt Nam
/// rẻ hơn khoảng 10 lần, và loại tin "Verify" không phải làm thủ tục đăng ký tên định danh — Twilio
/// vẫn bắt đăng ký sender ID với nhà mạng Việt Nam y như vậy.
///
/// Xác thực theo tài liệu SpeedSMS: HTTP Basic với token đóng vai username và mật khẩu là "x".
/// </summary>
public class SpeedSmsService(
    HttpClient httpClient,
    IOptions<SmsSettings> settings,
    IHostEnvironment environment,
    ILogger<SpeedSmsService> logger) : ISmsService
{
    private readonly SmsSettings _settings = settings.Value;

    public async Task SendAsync(string phoneNumber, string content, CancellationToken ct = default)
    {
        if (!_settings.IsConfigured)
        {
            // Ở Development thì in ra log cho tiện thử; ở môi trường thật thì PHẢI nổ. Im lặng bỏ qua
            // sẽ khiến bệnh nhân ngồi chờ mã không bao giờ tới mà không ai biết hệ thống đang hỏng.
            if (environment.IsDevelopment())
            {
                logger.LogWarning("[DEV-SMS] Gửi tới {Phone}: {Content}", phoneNumber, content);
                return;
            }

            throw new InvalidOperationException(
                "Chưa cấu hình SmsSettings:ApiToken — không gửi được SMS.");
        }

        var payload = new Dictionary<string, object?>
        {
            ["to"] = new[] { Normalize(phoneNumber) },
            ["content"] = content,
            ["sms_type"] = _settings.SmsType,
        };

        // sender chỉ có ý nghĩa (và bắt buộc) với brandname riêng; gửi kèm ở loại khác là thừa.
        if (_settings.SmsType == 3)
        {
            if (string.IsNullOrWhiteSpace(_settings.Sender))
                throw new InvalidOperationException(
                    "SmsSettings:SmsType = 3 (brandname riêng) nhưng thiếu SmsSettings:Sender.");

            payload["sender"] = _settings.Sender;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "sms/send")
        {
            Content = JsonContent.Create(payload),
        };

        // Token là username, mật khẩu cố định "x" — đúng như curl -u "{token}:x" trong tài liệu.
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ApiToken}:x")));

        var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"SpeedSMS trả về {(int)response.StatusCode}: {body}");

        // SpeedSMS trả HTTP 200 kèm status "error" trong body cho lỗi nghiệp vụ (hết tiền, số sai,
        // token hỏng...) — chỉ nhìn mã HTTP sẽ tưởng gửi thành công.
        EnsureProviderAccepted(body, phoneNumber);
    }

    private void EnsureProviderAccepted(string body, string phoneNumber)
    {
        string? status = null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("status", out var s))
                status = s.GetString();
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"SpeedSMS trả về nội dung không đọc được: {body}");
        }

        if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"SpeedSMS từ chối gửi tới {phoneNumber}: {body}");

        logger.LogInformation("Đã gửi SMS tới {Phone}", phoneNumber);
    }

    /// <summary>
    /// Chuẩn hóa về dạng +84 mà API chấp nhận. Số lưu trong hệ thống là dạng nội địa "0912345678",
    /// gửi nguyên như vậy vẫn được nhưng +84 là dạng dùng chung an toàn hơn khi có số quốc tế.
    /// </summary>
    private static string Normalize(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("84", StringComparison.Ordinal)) return $"+{digits}";
        if (digits.StartsWith('0')) return $"+84{digits[1..]}";

        return $"+84{digits}";
    }
}
