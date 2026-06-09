namespace DentalClinic.API.Domain.Enums;

public enum AppointmentStatus
{
    Pending,    // Chờ xác nhận
    Confirmed,  // Đã xác nhận
    Completed,  // Hoàn thành
    Cancelled   // Đã hủy
}
