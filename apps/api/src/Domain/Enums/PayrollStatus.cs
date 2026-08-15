namespace DentalClinic.API.Domain.Enums;

public enum PayrollStatus
{
    /// <summary>Còn dùng để đọc dữ liệu lịch sử cũ trước khi có quy trình Draft/Calculated/Approved — không còn được tạo mới.</summary>
    Pending,
    Draft,      // Kỳ lương mới tạo, số liệu còn sửa được (kể cả Thưởng)
    Calculated, // Đã tính lương, số liệu đã chốt — muốn sửa phải tính lại (quay về Draft)
    Approved,   // Owner đã duyệt — đủ điều kiện chi trả
    Paid,       // Đã thanh toán
}
