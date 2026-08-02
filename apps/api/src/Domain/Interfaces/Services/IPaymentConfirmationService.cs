using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Interfaces.Services;

/// <summary>
/// Quy tắc nghiệp vụ khi một khoản thanh toán được xác nhận — dùng chung giữa xác nhận thủ công
/// (nhân viên bấm "Đã thanh toán") và xác nhận qua webhook/đối soát cổng thanh toán, để không lặp lại
/// logic nghiệp vụ (đánh dấu Paid, tất toán hóa đơn gốc, credit công nợ liệu trình, hoàn tất lịch hẹn,
/// gửi notification) ở nhiều nơi.
/// </summary>
public interface IPaymentConfirmationService
{
    /// <summary>
    /// Áp dụng việc xác nhận thanh toán cho một hóa đơn. Idempotent — không làm gì nếu hóa đơn đã Paid.
    /// Không tự gọi SaveChanges — caller quyết định thời điểm commit.
    /// </summary>
    Task ConfirmInvoicePaymentAsync(Invoice invoice, PaymentMethod paymentMethod, CancellationToken ct);

    /// <summary>Đánh dấu một giao dịch cổng thanh toán thành công, rồi xác nhận thanh toán cho hóa đơn liên quan.</summary>
    Task ConfirmTransactionSuccessAsync(PaymentTransaction transaction, string? gatewayTransactionId, string rawPayload, CancellationToken ct);
}
