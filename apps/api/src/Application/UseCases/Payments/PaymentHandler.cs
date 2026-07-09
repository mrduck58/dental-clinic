using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Payments;

#region DTOs & Requests

public record CreatePaymentRequestRequest(string? Gateway);

public record PaymentTransactionDto(
    Guid Id,
    Guid InvoiceId,
    string Gateway,
    string Status,
    string GatewayOrderCode,
    decimal Amount,
    string? CheckoutUrl,
    string? QrCode,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt);

public record PaymentStatusDto(
    Guid InvoiceId,
    string InvoiceStatus,
    PaymentTransactionDto? LatestTransaction);

#endregion

/// <summary>
/// Điều phối việc tạo yêu cầu thanh toán qua cổng (PayOS...) và xử lý webhook trả về.
/// Quy tắc nghiệp vụ khi thanh toán được xác nhận (đánh dấu Paid, tất toán, hoàn tất lịch hẹn, notification)
/// vẫn nằm trong <see cref="InvoiceHandler.ApplyPaymentConfirmedAsync"/> — handler này chỉ lo phần giao dịch/cổng
/// thanh toán, tránh lặp lại logic nghiệp vụ ở hai nơi.
/// </summary>
public class PaymentHandler(
    AppDbContext dbContext,
    IPaymentGatewayResolver gatewayResolver,
    InvoiceHandler invoiceHandler)
{
    /// <summary>Tạo (hoặc tái sử dụng) một yêu cầu thanh toán cho hóa đơn qua cổng chỉ định.</summary>
    public async Task<PaymentTransactionDto> CreatePaymentRequestAsync(
        Guid invoiceId, PaymentGateway gateway, CancellationToken ct = default)
    {
        var invoice = await dbContext.Invoices
            .Include(i => i.Appointment).ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.Status == PaymentStatus.Paid)
            throw new ConflictException("Hóa đơn này đã được thanh toán.");

        // Tái sử dụng giao dịch Pending còn hiệu lực (nếu có) thay vì tạo link mới mỗi lần mở lại màn hình.
        var existing = await dbContext.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId && t.Gateway == gateway && t.Status == TransactionStatus.Pending
                        && (t.ExpiresAt == null || t.ExpiresAt > DateTimeOffset.UtcNow))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            return ToDto(existing);

        var gatewaySvc = gatewayResolver.Resolve(gateway);
        var patientName = invoice.Appointment.Patient.FullName;
        var orderCodeHint = $"{invoice.InvoiceNumber}-{Guid.NewGuid():N}"[..24];

        var linkResult = await gatewaySvc.CreatePaymentLinkAsync(new CreatePaymentLinkRequest(
            InvoiceId: invoice.Id,
            OrderCode: orderCodeHint,
            Amount: invoice.DepositAmount,
            Description: $"Thanh toan HD {invoice.InvoiceNumber}",
            BuyerName: patientName,
            ReturnUrl: null,
            CancelUrl: null), ct);

        var transaction = PaymentTransaction.Create(
            invoice.Id, gateway, linkResult.GatewayOrderCode, invoice.DepositAmount,
            linkResult.CheckoutUrl, linkResult.QrCode, linkResult.RawResponsePayload, linkResult.ExpiresAt);

        dbContext.PaymentTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(ct);

        return ToDto(transaction);
    }

    /// <summary>Xử lý webhook từ cổng thanh toán — idempotent, an toàn khi gọi lại nhiều lần cho cùng 1 giao dịch.</summary>
    public async Task HandleWebhookAsync(
        PaymentGateway gateway, string rawPayload, IReadOnlyDictionary<string, string> headers, CancellationToken ct = default)
    {
        var gatewaySvc = gatewayResolver.Resolve(gateway);
        var verified = await gatewaySvc.VerifyAndParseWebhookAsync(rawPayload, headers, ct);

        if (!verified.IsValid)
            throw new ValidationException("Webhook signature không hợp lệ.");

        var transaction = await dbContext.PaymentTransactions
            .Include(t => t.Invoice).ThenInclude(i => i.Appointment)
            .FirstOrDefaultAsync(t => t.Gateway == gateway && t.GatewayOrderCode == verified.GatewayOrderCode, ct);
        if (transaction is null) return; // Order code không xác định — log phía gateway, trả 200 để gateway dừng retry.

        if (transaction.IsTerminal) return; // Webhook trùng lặp — đã xử lý trước đó, không làm gì thêm.

        if (verified.IsSuccess)
        {
            // Đối chiếu số tiền trước khi xác nhận — lệch số tiền coi là bất thường, không tự động Paid.
            if (verified.Amount > 0 && Math.Abs(verified.Amount - transaction.Amount) > 1)
            {
                transaction.MarkFailed($"Số tiền không khớp: webhook={verified.Amount}, giao dịch={transaction.Amount}", rawPayload);
                await dbContext.SaveChangesAsync(ct);
                return;
            }

            transaction.MarkSuccess(verified.GatewayTransactionId, rawPayload);
            await dbContext.SaveChangesAsync(ct);

            var paymentMethod = gateway == PaymentGateway.PayOS && transaction.Invoice.PaymentMethod == PaymentMethod.BankTransfer
                ? PaymentMethod.BankTransfer
                : PaymentMethod.OnlinePayment;

            await invoiceHandler.ApplyPaymentConfirmedAsync(transaction.Invoice, paymentMethod, ct);
            await dbContext.SaveChangesAsync(ct);
        }
        else
        {
            transaction.MarkFailed(verified.FailureReason ?? "Cổng thanh toán báo thất bại.", rawPayload);
            await dbContext.SaveChangesAsync(ct);
        }
    }

    /// <summary>Trạng thái thanh toán hiện tại của hóa đơn — dùng cho polling từ admin website / mobile app.</summary>
    public async Task<PaymentStatusDto> GetStatusAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        var latest = await dbContext.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new PaymentStatusDto(invoice.Id, invoice.Status.ToString(), latest is null ? null : ToDto(latest));
    }

    private static PaymentTransactionDto ToDto(PaymentTransaction t) => new(
        t.Id, t.InvoiceId, t.Gateway.ToString(), t.Status.ToString(), t.GatewayOrderCode, t.Amount,
        t.CheckoutUrl, t.QrCode, t.CreatedAt, t.ExpiresAt);
}
