namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>
/// Chốt các thay đổi đã stage qua repository trong 1 lần lưu duy nhất. Chỉ dùng cho các luồng
/// xuyên nhiều entity trong cùng 1 giao dịch nghiệp vụ (ví dụ: xác nhận thanh toán chạm vào
/// Invoice + PaymentTransaction + TreatmentPlan + Appointment) — nơi tách SaveChanges vào từng
/// repository riêng lẻ sẽ làm mất tính nguyên tử. Các repository CRUD đơn-entity khác vẫn tự
/// SaveChanges bên trong như trước, không cần đi qua interface này.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Chữ ký khớp nguyên với <c>DbContext.SaveChangesAsync</c> (trả số dòng bị ảnh hưởng)
    /// để <c>AppDbContext</c> implement trực tiếp, không cần lớp bọc.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
