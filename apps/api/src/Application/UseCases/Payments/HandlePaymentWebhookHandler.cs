using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.API.Application.UseCases.Payments;

public record HandlePaymentWebhookCommand(
    PaymentGateway Gateway, string RawPayload, IReadOnlyDictionary<string, string> Headers) : IRequest;

/// <summary>Xử lý webhook từ cổng thanh toán — idempotent, an toàn khi gọi lại nhiều lần cho cùng 1 giao dịch.</summary>
public class HandlePaymentWebhookHandler(
    AppDbContext dbContext,
    IPaymentGatewayResolver gatewayResolver,
    IPaymentConfirmationService paymentConfirmationService,
    ILogger<HandlePaymentWebhookHandler> logger) : IRequestHandler<HandlePaymentWebhookCommand>
{
    public async Task Handle(HandlePaymentWebhookCommand command, CancellationToken ct)
    {
        // Log ngay khi request tới nơi (trước khi verify) — để phân biệt "PayOS chưa từng gọi vào" với
        // "gọi vào rồi nhưng bị từ chối" (sai signature, thiếu cấu hình...) khi tra log sự cố.
        logger.LogInformation(
            "Nhận webhook thanh toán từ {Gateway}, kích thước payload={PayloadLength} bytes.", command.Gateway, command.RawPayload.Length);

        var gatewaySvc = gatewayResolver.Resolve(command.Gateway);
        var verified = await gatewaySvc.VerifyAndParseWebhookAsync(command.RawPayload, command.Headers, ct);

        if (!verified.IsValid)
            throw new ValidationException("Webhook signature không hợp lệ.");

        var transaction = await dbContext.PaymentTransactions
            .Include(t => t.Invoice).ThenInclude(i => i.Appointment)
            .FirstOrDefaultAsync(t => t.Gateway == command.Gateway && t.GatewayOrderCode == verified.GatewayOrderCode, ct);
        if (transaction is null)
        {
            // Order code không xác định — trả 200 để gateway dừng retry, nhưng vẫn log rõ để dễ tra soát.
            logger.LogWarning(
                "Webhook {Gateway} với orderCode={OrderCode} không khớp giao dịch nào trong hệ thống.",
                command.Gateway, verified.GatewayOrderCode);
            return;
        }

        if (transaction.IsTerminal) return; // Webhook trùng lặp — đã xử lý trước đó, không làm gì thêm.

        if (verified.IsSuccess)
        {
            // Đối chiếu số tiền trước khi xác nhận — lệch số tiền coi là bất thường, không tự động Paid.
            if (verified.Amount > 0 && Math.Abs(verified.Amount - transaction.Amount) > 1)
            {
                transaction.MarkFailed($"Số tiền không khớp: webhook={verified.Amount}, giao dịch={transaction.Amount}", command.RawPayload);
                await dbContext.SaveChangesAsync(ct);
                return;
            }

            await paymentConfirmationService.ConfirmTransactionSuccessAsync(
                transaction, verified.GatewayTransactionId, command.RawPayload, ct);
        }
        else
        {
            transaction.MarkFailed(verified.FailureReason ?? "Cổng thanh toán báo thất bại.", command.RawPayload);
            await dbContext.SaveChangesAsync(ct);
        }
    }
}
