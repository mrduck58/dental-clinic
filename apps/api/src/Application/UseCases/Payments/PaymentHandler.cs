using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
    InvoiceHandler invoiceHandler,
    IConfiguration configuration,
    ILogger<PaymentHandler> logger)
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

        // Return/cancel URL đưa người dùng quay lại app sau khi thanh toán trên trang PayOS — CHỈ ảnh hưởng
        // trải nghiệm redirect trên trình duyệt, không phải kênh cập nhật trạng thái (đó là webhook, xử lý riêng
        // ở HandleWebhookAsync). Không được để null: PayOSGatewayService sẽ fallback sang BaseUrl của PayOS API.
        var frontendBase = (configuration["FrontendBaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var linkResult = await gatewaySvc.CreatePaymentLinkAsync(new CreatePaymentLinkRequest(
            InvoiceId: invoice.Id,
            OrderCode: orderCodeHint,
            Amount: invoice.DepositAmount,
            Description: $"Thanh toan HD {invoice.InvoiceNumber}",
            BuyerName: patientName,
            ReturnUrl: $"{frontendBase}/payment-result?invoiceId={invoice.Id}&status=success",
            CancelUrl: $"{frontendBase}/payment-result?invoiceId={invoice.Id}&status=cancel"), ct);

        // Kiểm tra lại lần nữa NGAY TRƯỚC khi insert: lệnh gọi PayOS ở trên có độ trễ mạng đáng kể, đủ để một
        // request khác (2 tab, hoặc React StrictMode gọi effect 2 lần ở dev) lọt qua nhánh "chưa có Pending" phía
        // trên và đã insert xong trong lúc ta đang chờ PayOS trả lời — thu hẹp cửa sổ race, không để tạo 2 giao dịch.
        var raceWinner = await dbContext.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId && t.Gateway == gateway && t.Status == TransactionStatus.Pending
                        && (t.ExpiresAt == null || t.ExpiresAt > DateTimeOffset.UtcNow))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (raceWinner is not null)
            return ToDto(raceWinner);

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
        // Log ngay khi request tới nơi (trước khi verify) — để phân biệt "PayOS chưa từng gọi vào" với
        // "gọi vào rồi nhưng bị từ chối" (sai signature, thiếu cấu hình...) khi tra log sự cố.
        logger.LogInformation(
            "Nhận webhook thanh toán từ {Gateway}, kích thước payload={PayloadLength} bytes.", gateway, rawPayload.Length);

        var gatewaySvc = gatewayResolver.Resolve(gateway);
        var verified = await gatewaySvc.VerifyAndParseWebhookAsync(rawPayload, headers, ct);

        if (!verified.IsValid)
            throw new ValidationException("Webhook signature không hợp lệ.");

        var transaction = await dbContext.PaymentTransactions
            .Include(t => t.Invoice).ThenInclude(i => i.Appointment)
            .FirstOrDefaultAsync(t => t.Gateway == gateway && t.GatewayOrderCode == verified.GatewayOrderCode, ct);
        if (transaction is null)
        {
            // Order code không xác định — trả 200 để gateway dừng retry, nhưng vẫn log rõ để dễ tra soát.
            logger.LogWarning(
                "Webhook {Gateway} với orderCode={OrderCode} không khớp giao dịch nào trong hệ thống.",
                gateway, verified.GatewayOrderCode);
            return;
        }

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

            await ConfirmTransactionSuccessAsync(transaction, verified.GatewayTransactionId, rawPayload, ct);
        }
        else
        {
            transaction.MarkFailed(verified.FailureReason ?? "Cổng thanh toán báo thất bại.", rawPayload);
            await dbContext.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Trạng thái thanh toán hiện tại của hóa đơn — dùng cho polling từ admin website / mobile app.
    /// Chủ động hỏi lại cổng thanh toán (đối soát dự phòng) cho MỌI giao dịch còn Pending của hóa đơn — không chỉ
    /// giao dịch mới nhất — phòng khi webhook chưa tới/bị từ chối, VÀ phòng trường hợp hóa đơn từng bị tạo trùng
    /// 2 giao dịch Pending song song (do race tạo yêu cầu thanh toán) mà người dùng lại thanh toán ở giao dịch
    /// không phải giao dịch mới nhất — nếu chỉ kiểm tra giao dịch mới nhất sẽ bỏ sót, hóa đơn kẹt mãi ở Unpaid dù
    /// PayOS đã báo giao dịch kia hoàn tất.
    /// </summary>
    public async Task<PaymentStatusDto> GetStatusAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        var pendingTxns = await dbContext.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId && t.Status == TransactionStatus.Pending)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
        foreach (var pending in pendingTxns)
            await ReconcileWithGatewayAsync(pending, ct);

        var latest = await dbContext.PaymentTransactions
            .Where(t => t.InvoiceId == invoiceId)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new PaymentStatusDto(invoice.Id, invoice.Status.ToString(), latest is null ? null : ToDto(latest));
    }

    /// <summary>
    /// Hỏi thẳng cổng thanh toán trạng thái thật của 1 giao dịch còn Pending, để tự phục hồi khi webhook lỗi —
    /// xử lý cả 2 chiều: tự xác nhận thành công (webhook thanh toán chưa/không tới), VÀ tự đánh Failed khi người
    /// dùng đã hủy/link hết hạn bên PayOS (nhiều cổng thanh toán KHÔNG gửi webhook cho trường hợp hủy, nên nếu
    /// không tự đối soát chiều này, giao dịch sẽ kẹt vĩnh viễn ở Pending dù người dùng đã bấm hủy từ lâu).
    /// </summary>
    private async Task<PaymentTransaction> ReconcileWithGatewayAsync(PaymentTransaction pending, CancellationToken ct)
    {
        try
        {
            var gatewaySvc = gatewayResolver.Resolve(pending.Gateway);
            var remote = await gatewaySvc.GetTransactionStatusAsync(pending.GatewayOrderCode, ct);
            if (remote is not { IsValid: true }) return pending;

            // Cổng thanh toán xác nhận vẫn đang PENDING thật sự — chưa có gì để cập nhật, thử lại ở lượt poll sau.
            if (!remote.IsSuccess && string.Equals(remote.FailureReason, "PENDING", StringComparison.OrdinalIgnoreCase))
                return pending;

            var transaction = await dbContext.PaymentTransactions
                .Include(t => t.Invoice).ThenInclude(i => i.Appointment)
                .FirstAsync(t => t.Id == pending.Id, ct);
            if (transaction.IsTerminal) return transaction; // webhook đã xử lý xong trong lúc ta đang hỏi cổng thanh toán

            if (remote.IsSuccess)
            {
                logger.LogInformation(
                    "Đối soát dự phòng: giao dịch {TransactionId} thực tế đã thành công trên {Gateway} nhưng webhook chưa/không tới.",
                    transaction.Id, transaction.Gateway);
                await ConfirmTransactionSuccessAsync(transaction, remote.GatewayTransactionId, remote.RawPayload, ct);
            }
            else
            {
                logger.LogInformation(
                    "Đối soát dự phòng: giao dịch {TransactionId} đã bị hủy/hết hạn trên {Gateway} (trạng thái: {Status}) nhưng webhook chưa/không tới.",
                    transaction.Id, transaction.Gateway, remote.FailureReason);
                transaction.MarkFailed(remote.FailureReason ?? "Giao dịch đã bị hủy hoặc hết hạn.", remote.RawPayload);
                await dbContext.SaveChangesAsync(ct);
            }
            return transaction;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Đối soát trạng thái giao dịch {TransactionId} với cổng thanh toán thất bại.", pending.Id);
            return pending;
        }
    }

    private async Task ConfirmTransactionSuccessAsync(
        PaymentTransaction transaction, string? gatewayTransactionId, string rawPayload, CancellationToken ct)
    {
        transaction.MarkSuccess(gatewayTransactionId, rawPayload);
        await dbContext.SaveChangesAsync(ct);

        var paymentMethod = transaction.Gateway == PaymentGateway.PayOS && transaction.Invoice.PaymentMethod == PaymentMethod.BankTransfer
            ? PaymentMethod.BankTransfer
            : PaymentMethod.OnlinePayment;

        // ApplyPaymentConfirmedAsync tự đóng mọi giao dịch Pending còn sót lại của hóa đơn này (kể cả các giao
        // dịch trùng do race trước khi sửa) — không cần lặp lại việc đó ở đây.
        await invoiceHandler.ApplyPaymentConfirmedAsync(transaction.Invoice, paymentMethod, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    private static PaymentTransactionDto ToDto(PaymentTransaction t) => new(
        t.Id, t.InvoiceId, t.Gateway.ToString(), t.Status.ToString(), t.GatewayOrderCode, t.Amount,
        t.CheckoutUrl, t.QrCode, t.CreatedAt, t.ExpiresAt);
}
