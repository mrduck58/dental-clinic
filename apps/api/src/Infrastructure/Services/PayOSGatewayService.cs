using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.API.Infrastructure.Services;

/// <summary>
/// Tích hợp PayOS (https://payos.vn) — dùng cho cả VietQR chuyển khoản (PaymentMethod.BankTransfer)
/// và thanh toán online trong app (PaymentMethod.OnlinePayment).
///
/// Trạng thái xác minh (2026-07-07):
/// - CreatePaymentLinkAsync: đã test trực tiếp với credentials thật của PayOS (không phải sandbox — PayOS
///   không có sandbox riêng) qua cả unit test độc lập và qua toàn bộ stack (API → PaymentHandler →
///   PayOSGatewayService), tạo được checkoutUrl/qrCode thật, PayOS trả code "00"/"success".
/// - VerifyAndParseWebhookAsync: thuật toán (HMAC-SHA256, ký trên TẤT CẢ field của "data" sắp theo alphabet,
///   null→"") đã đối chiếu với mẫu code chính thức tại https://payos.vn/docs/tich-hop-webhook/kiem-tra-du-lieu-voi-signature/,
///   nhưng CHƯA test được với webhook thật từ PayOS (cần cấu hình webhook URL public — qua ngrok khi dev local —
///   trên dashboard PayOS rồi thực hiện một thanh toán thật để xác nhận).
/// </summary>
public class PayOSGatewayService(
    HttpClient httpClient,
    IOptions<PayOSSettings> options,
    ILogger<PayOSGatewayService> logger) : IPaymentGatewayService
{
    private readonly PayOSSettings _settings = options.Value;

    public PaymentGateway Gateway => PaymentGateway.PayOS;

    public async Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        CreatePaymentLinkRequest request, CancellationToken ct = default)
    {
        EnsureConfigured();

        // PayOS yêu cầu orderCode là số nguyên. Dùng mốc thời gian (ms) — đơn điệu tăng, gần như không trùng;
        // lớp gọi (PaymentHandler) đã đảm bảo không tạo 2 giao dịch Pending song song cho cùng 1 hóa đơn.
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var amount = (long)Math.Round(request.Amount, MidpointRounding.AwayFromZero);
        var description = Truncate(request.Description, 25); // PayOS giới hạn description ~25 ký tự

        var payload = new PayOSCreateRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            Description = description,
            BuyerName = request.BuyerName,
            CancelUrl = request.CancelUrl ?? _settings.BaseUrl,
            ReturnUrl = request.ReturnUrl ?? _settings.BaseUrl,
        };
        payload.Signature = ComputeCreateSignature(orderCode, amount, description, payload.CancelUrl, payload.ReturnUrl);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/payment-requests")
        {
            Content = JsonContent.Create(payload)
        };
        httpRequest.Headers.Add("x-client-id", _settings.ClientId);
        httpRequest.Headers.Add("x-api-key", _settings.ApiKey);

        using var response = await httpClient.SendAsync(httpRequest, ct);
        var rawBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("PayOS create-payment-link thất bại ({StatusCode}): {Body}", response.StatusCode, rawBody);
            throw new ValidationException("Không thể tạo yêu cầu thanh toán qua PayOS.");
        }

        var parsed = JsonSerializer.Deserialize<PayOSEnvelope<PayOSCreateResponseData>>(rawBody, JsonOpts)
            ?? throw new ValidationException("Phản hồi không hợp lệ từ PayOS.");

        return new CreatePaymentLinkResult(
            CheckoutUrl: parsed.Data?.CheckoutUrl ?? string.Empty,
            QrCode: parsed.Data?.QrCode,
            GatewayOrderCode: orderCode.ToString(),
            ExpiresAt: null, // PayOS không luôn trả expiredAt trong response tạo link — có thể bổ sung sau khi xác nhận với docs
            RawResponsePayload: rawBody);
    }

    public Task<WebhookVerificationResult> VerifyAndParseWebhookAsync(
        string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        EnsureConfigured();

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(rawPayload);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "PayOS webhook: payload không parse được JSON.");
            return Task.FromResult(new WebhookVerificationResult(false, false, "", null, 0, rawPayload, "Payload không hợp lệ."));
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("signature", out var signatureProp))
                return Task.FromResult(new WebhookVerificationResult(false, false, "", null, 0, rawPayload, "Thiếu data/signature."));

            var signature = signatureProp.GetString() ?? "";
            var orderCode = data.TryGetProperty("orderCode", out var ocEl) ? ocEl.ToString() : "";

            // Ký trên TẤT CẢ field của object "data" (không chỉ vài field cố định), sắp xếp theo alphabet,
            // dạng key=value&key=value — đúng theo mẫu code chính thức của PayOS (null/undefined → "").
            var expectedSignature = ComputeWebhookSignature(data);
            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));

            if (!isValid)
            {
                logger.LogWarning("PayOS webhook: signature không khớp. orderCode={OrderCode}", orderCode);
                return Task.FromResult(new WebhookVerificationResult(false, false, orderCode, null, 0, rawPayload, "Signature không hợp lệ."));
            }

            var amount = data.TryGetProperty("amount", out var amountEl) && amountEl.TryGetDecimal(out var amountVal) ? amountVal : 0m;
            var reference = data.TryGetProperty("reference", out var refEl) ? refEl.GetString() : null;
            var code = data.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : null;
            var desc = data.TryGetProperty("desc", out var descEl) ? descEl.GetString() : null;

            // "00" = thành công theo tài liệu PayOS. Các mã khác coi là thất bại/hủy.
            var isSuccess = code == "00";

            return Task.FromResult(new WebhookVerificationResult(
                IsValid: true,
                IsSuccess: isSuccess,
                GatewayOrderCode: orderCode,
                GatewayTransactionId: reference,
                Amount: amount,
                RawPayload: rawPayload,
                FailureReason: isSuccess ? null : desc));
        }
    }

    public async Task<WebhookVerificationResult?> GetTransactionStatusAsync(
        string gatewayOrderCode, CancellationToken ct = default)
    {
        EnsureConfigured();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/v2/payment-requests/{gatewayOrderCode}");
        httpRequest.Headers.Add("x-client-id", _settings.ClientId);
        httpRequest.Headers.Add("x-api-key", _settings.ApiKey);

        using var response = await httpClient.SendAsync(httpRequest, ct);
        if (!response.IsSuccessStatusCode) return null;

        var rawBody = await response.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<PayOSEnvelope<PayOSStatusResponseData>>(rawBody, JsonOpts);
        if (parsed?.Data is null) return null;

        var isSuccess = string.Equals(parsed.Data.Status, "PAID", StringComparison.OrdinalIgnoreCase);
        return new WebhookVerificationResult(
            IsValid: true,
            IsSuccess: isSuccess,
            GatewayOrderCode: gatewayOrderCode,
            GatewayTransactionId: null,
            Amount: parsed.Data.Amount,
            RawPayload: rawBody,
            FailureReason: isSuccess ? null : parsed.Data.Status);
    }

    private void EnsureConfigured()
    {
        if (!_settings.IsConfigured)
            throw new ValidationException("Cổng thanh toán PayOS chưa được cấu hình (thiếu ClientId/ApiKey/ChecksumKey).");
    }

    private string ComputeCreateSignature(long orderCode, long amount, string description, string cancelUrl, string returnUrl)
    {
        // Theo docs PayOS: ký trên 5 field, sắp xếp theo thứ tự alphabet của tên field.
        var data = $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";
        return HmacSha256Hex(data, _settings.ChecksumKey);
    }

    /// <summary>
    /// Ký trên TẤT CẢ field hiện có trong object "data" của webhook (không phải một danh sách cố định) —
    /// sắp xếp theo alphabet (ordinal), null/undefined → chuỗi rỗng. Theo đúng mẫu code chính thức của PayOS
    /// (https://payos.vn/docs/tich-hop-webhook/kiem-tra-du-lieu-voi-signature/), để không bị lệch khi PayOS
    /// thêm field mới vào payload trong tương lai.
    /// </summary>
    private string ComputeWebhookSignature(JsonElement data)
    {
        var raw = string.Join('&', data.EnumerateObject()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}={StringifyJsonValue(p.Value)}"));
        return HmacSha256Hex(raw, _settings.ChecksumKey);
    }

    private static string StringifyJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => "",
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        // Number/Object/Array: dùng nguyên văn JSON — với Number khớp đúng chữ số PayOS đã gửi.
        _ => value.GetRawText(),
    };

    private static string HmacSha256Hex(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── PayOS wire DTOs ──────────────────────────────────────────────────────

    private class PayOSCreateRequest
    {
        [JsonPropertyName("orderCode")] public long OrderCode { get; set; }
        [JsonPropertyName("amount")] public long Amount { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; } = "";
        [JsonPropertyName("buyerName")] public string? BuyerName { get; set; }
        [JsonPropertyName("cancelUrl")] public string CancelUrl { get; set; } = "";
        [JsonPropertyName("returnUrl")] public string ReturnUrl { get; set; } = "";
        [JsonPropertyName("signature")] public string Signature { get; set; } = "";
    }

    private class PayOSEnvelope<T>
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("desc")] public string? Desc { get; set; }
        [JsonPropertyName("data")] public T? Data { get; set; }
    }

    private class PayOSCreateResponseData
    {
        [JsonPropertyName("checkoutUrl")] public string? CheckoutUrl { get; set; }
        [JsonPropertyName("qrCode")] public string? QrCode { get; set; }
        [JsonPropertyName("orderCode")] public long OrderCode { get; set; }
    }

    private class PayOSStatusResponseData
    {
        [JsonPropertyName("status")] public string? Status { get; set; } // PENDING | PAID | CANCELLED | EXPIRED
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
    }
}
