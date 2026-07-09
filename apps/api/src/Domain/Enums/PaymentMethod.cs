namespace DentalClinic.API.Domain.Enums;

public enum PaymentMethod
{
    Cash,           // Tiền mặt
    BankTransfer,   // Chuyển khoản
    OnlinePayment   // Thanh toán online (Momo, VNPay, Stripe...)
}

public enum PaymentStatus
{
    Unpaid,   // Chưa thanh toán
    Paid,     // Đã thanh toán
    Refunded  // Đã hoàn tiền
}

public enum PaymentType
{
    Full,     // Thanh toán toàn bộ
    Deposit   // Đặt cọc (thanh toán một phần)
}

public enum PaymentGateway
{
    PayOS   // VietQR chuyển khoản + thanh toán online. Thêm gateway khác (VNPay, Momo...) tại đây.
}

public enum TransactionStatus
{
    Pending,    // Đã tạo link/QR, đang chờ thanh toán
    Success,    // Cổng thanh toán xác nhận thành công (qua webhook)
    Failed,     // Cổng thanh toán báo lỗi/thất bại
    Cancelled,  // Người dùng/hệ thống hủy giao dịch
    Expired     // Link/QR đã hết hạn mà chưa thanh toán
}
