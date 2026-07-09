using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Interfaces.Services;

public record CreatePaymentLinkRequest(
    Guid InvoiceId,
    string OrderCode,
    decimal Amount,
    string Description,
    string? BuyerName,
    string? ReturnUrl,
    string? CancelUrl);

public record CreatePaymentLinkResult(
    string CheckoutUrl,
    string? QrCode,
    string GatewayOrderCode,
    DateTimeOffset? ExpiresAt,
    string RawResponsePayload);

public record WebhookVerificationResult(
    bool IsValid,
    bool IsSuccess,
    string GatewayOrderCode,
    string? GatewayTransactionId,
    decimal Amount,
    string RawPayload,
    string? FailureReason);

/// <summary>
/// Trừu tượng hóa một cổng thanh toán (PayOS, và sau này VNPay/Momo...) để phần còn lại của hệ thống
/// (tạo yêu cầu thanh toán, xử lý webhook) không phụ thuộc vào cổng cụ thể nào.
/// </summary>
public interface IPaymentGatewayService
{
    PaymentGateway Gateway { get; }

    Task<CreatePaymentLinkResult> CreatePaymentLinkAsync(
        CreatePaymentLinkRequest request, CancellationToken ct = default);

    /// <summary>Xác thực chữ ký/checksum và chuẩn hóa payload webhook thô về kết quả chung cho mọi cổng.</summary>
    Task<WebhookVerificationResult> VerifyAndParseWebhookAsync(
        string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default);

    /// <summary>Tra cứu trạng thái trực tiếp từ cổng thanh toán — dùng khi polling mà chưa có webhook.</summary>
    Task<WebhookVerificationResult?> GetTransactionStatusAsync(
        string gatewayOrderCode, CancellationToken ct = default);
}

/// <summary>Chọn đúng implementation của <see cref="IPaymentGatewayService"/> theo <see cref="PaymentGateway"/>.</summary>
public interface IPaymentGatewayResolver
{
    IPaymentGatewayService Resolve(PaymentGateway gateway);
}
