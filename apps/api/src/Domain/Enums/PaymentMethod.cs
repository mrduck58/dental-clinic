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
