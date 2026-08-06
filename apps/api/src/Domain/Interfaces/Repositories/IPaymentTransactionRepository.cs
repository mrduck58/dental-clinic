using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IPaymentTransactionRepository
{
    /// <summary>Giao dịch Pending còn hiệu lực (chưa hết hạn) gần nhất của hóa đơn trên 1 cổng thanh toán —
    /// để tái sử dụng thay vì tạo link mới mỗi lần mở lại màn hình.</summary>
    Task<PaymentTransaction?> GetLatestPendingAsync(Guid invoiceId, PaymentGateway gateway, CancellationToken ct = default);

    /// <summary>Mọi giao dịch còn Pending của 1 hóa đơn, có tracking — dùng đóng/đối soát.</summary>
    Task<IReadOnlyList<PaymentTransaction>> GetPendingByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default);

    Task<PaymentTransaction?> GetLatestByInvoiceIdAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>Đọc để ghi (tracking) kèm Invoice→Appointment — dùng đối soát/webhook cần hoàn tất buổi hẹn.</summary>
    Task<PaymentTransaction?> GetByIdWithInvoiceAndAppointmentAsync(Guid id, CancellationToken ct = default);

    /// <summary>Đọc để ghi (tracking) kèm Invoice→Appointment theo mã đơn hàng của cổng thanh toán — dùng xử lý webhook.</summary>
    Task<PaymentTransaction?> GetByGatewayOrderCodeWithInvoiceAndAppointmentAsync(
        PaymentGateway gateway, string gatewayOrderCode, CancellationToken ct = default);

    /// <summary>Thêm giao dịch mới — chỉ stage, KHÔNG tự SaveChanges (caller dùng <see cref="IUnitOfWork"/>).</summary>
    void Add(PaymentTransaction transaction);
}
