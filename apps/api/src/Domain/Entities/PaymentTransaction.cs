using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Entities;

/// <summary>
/// Một lần thử thanh toán qua cổng thanh toán (VietQR chuyển khoản hoặc thanh toán online) cho một hóa đơn.
/// Một hóa đơn có thể có nhiều giao dịch (thử lại, hết hạn...); trạng thái tổng của hóa đơn vẫn nằm ở <see cref="Invoice.Status"/>.
/// </summary>
public class PaymentTransaction
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }

    public PaymentGateway Gateway { get; private set; }
    public string GatewayOrderCode { get; private set; } = string.Empty; // Mã tham chiếu của chúng ta gửi cho cổng thanh toán
    public string? GatewayTransactionId { get; private set; }            // Mã giao dịch của cổng thanh toán (điền khi webhook về)
    public string? CheckoutUrl { get; private set; }
    public string? QrCode { get; private set; }

    public decimal Amount { get; private set; }
    public TransactionStatus Status { get; private set; }

    public string? RawCreateResponsePayload { get; private set; } // JSON trả về khi tạo link — lưu để đối soát
    public string? RawWebhookPayload { get; private set; }        // JSON của webhook gần nhất — lưu để đối soát/tra soát tranh chấp
    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public Invoice Invoice { get; private set; } = null!;

    private PaymentTransaction() { }

    public static PaymentTransaction Create(
        Guid invoiceId,
        PaymentGateway gateway,
        string gatewayOrderCode,
        decimal amount,
        string? checkoutUrl,
        string? qrCode,
        string? rawCreateResponsePayload,
        DateTimeOffset? expiresAt)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Gateway = gateway,
            GatewayOrderCode = gatewayOrderCode,
            Amount = amount,
            CheckoutUrl = checkoutUrl,
            QrCode = qrCode,
            RawCreateResponsePayload = rawCreateResponsePayload,
            ExpiresAt = expiresAt,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Không làm gì nếu giao dịch đã ở trạng thái cuối — chống xử lý trùng khi webhook gọi lại nhiều lần.</summary>
    public bool IsTerminal =>
        Status is TransactionStatus.Success or TransactionStatus.Failed
            or TransactionStatus.Cancelled or TransactionStatus.Expired;

    public void MarkSuccess(string? gatewayTransactionId, string rawWebhookPayload)
    {
        if (IsTerminal) return;
        Status = TransactionStatus.Success;
        GatewayTransactionId = gatewayTransactionId;
        RawWebhookPayload = rawWebhookPayload;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason, string rawWebhookPayload)
    {
        if (IsTerminal) return;
        Status = TransactionStatus.Failed;
        FailureReason = reason;
        RawWebhookPayload = rawWebhookPayload;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled(string rawWebhookPayload)
    {
        if (IsTerminal) return;
        Status = TransactionStatus.Cancelled;
        RawWebhookPayload = rawWebhookPayload;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        if (IsTerminal) return;
        Status = TransactionStatus.Expired;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
