using DentalClinic.API.Application.DTOs.Payments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using static DentalClinic.API.Application.UseCases.Payments.PaymentHelpers;

namespace DentalClinic.API.Application.UseCases.Payments;

public record GetPaymentStatusQuery(Guid InvoiceId) : IRequest<PaymentStatusDto>;

/// <summary>
/// Trạng thái thanh toán hiện tại của hóa đơn — dùng cho polling từ admin website / mobile app.
/// Chủ động hỏi lại cổng thanh toán (đối soát dự phòng) cho MỌI giao dịch còn Pending của hóa đơn — không chỉ
/// giao dịch mới nhất — phòng khi webhook chưa tới/bị từ chối, VÀ phòng trường hợp hóa đơn từng bị tạo trùng
/// 2 giao dịch Pending song song (do race tạo yêu cầu thanh toán) mà người dùng lại thanh toán ở giao dịch
/// không phải giao dịch mới nhất — nếu chỉ kiểm tra giao dịch mới nhất sẽ bỏ sót, hóa đơn kẹt mãi ở Unpaid dù
/// PayOS đã báo giao dịch kia hoàn tất.
/// </summary>
public class GetPaymentStatusHandler(
    IInvoiceRepository invoiceRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IUnitOfWork unitOfWork,
    IPaymentGatewayResolver gatewayResolver,
    IPaymentConfirmationService paymentConfirmationService,
    ILogger<GetPaymentStatusHandler> logger) : IRequestHandler<GetPaymentStatusQuery, PaymentStatusDto>
{
    public async Task<PaymentStatusDto> Handle(GetPaymentStatusQuery query, CancellationToken ct)
    {
        var invoice = await invoiceRepository.GetByIdAsync(query.InvoiceId, ct)
            ?? throw new NotFoundException("Không tìm thấy hóa đơn.");

        var pendingTxns = await paymentTransactionRepository.GetPendingByInvoiceIdAsync(query.InvoiceId, ct);
        foreach (var pending in pendingTxns)
            await ReconcileWithGatewayAsync(pending, ct);

        var latest = await paymentTransactionRepository.GetLatestByInvoiceIdAsync(query.InvoiceId, ct);

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

            var transaction = await paymentTransactionRepository.GetByIdWithInvoiceAndAppointmentAsync(pending.Id, ct)
                ?? throw new InvalidOperationException($"Không tìm thấy giao dịch {pending.Id}.");
            if (transaction.IsTerminal) return transaction; // webhook đã xử lý xong trong lúc ta đang hỏi cổng thanh toán

            if (remote.IsSuccess)
            {
                logger.LogInformation(
                    "Đối soát dự phòng: giao dịch {TransactionId} thực tế đã thành công trên {Gateway} nhưng webhook chưa/không tới.",
                    transaction.Id, transaction.Gateway);
                await paymentConfirmationService.ConfirmTransactionSuccessAsync(
                    transaction, remote.GatewayTransactionId, remote.RawPayload, ct);
            }
            else
            {
                logger.LogInformation(
                    "Đối soát dự phòng: giao dịch {TransactionId} đã bị hủy/hết hạn trên {Gateway} (trạng thái: {Status}) nhưng webhook chưa/không tới.",
                    transaction.Id, transaction.Gateway, remote.FailureReason);
                transaction.MarkFailed(remote.FailureReason ?? "Giao dịch đã bị hủy hoặc hết hạn.", remote.RawPayload);
                await unitOfWork.SaveChangesAsync(ct);
            }
            return transaction;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Đối soát trạng thái giao dịch {TransactionId} với cổng thanh toán thất bại.", pending.Id);
            return pending;
        }
    }
}
