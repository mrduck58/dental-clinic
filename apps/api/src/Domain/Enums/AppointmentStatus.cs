namespace DentalClinic.API.Domain.Enums;

public enum AppointmentStatus
{
    Pending,     // Chờ xác nhận
    Confirmed,   // Đã xác nhận (bệnh nhân đã đặt, chờ đến khám)
    CheckedIn,   // Đã check-in (bệnh nhân đã đến, chờ khám)
    InProgress,     // Đang khám (bác sĩ bắt đầu điều trị)
    PendingPayment, // Chờ thanh toán (kết thúc điều trị)
    Completed,      // Hoàn thành (đã thanh toán)
    Cancelled       // Đã hủy
}
