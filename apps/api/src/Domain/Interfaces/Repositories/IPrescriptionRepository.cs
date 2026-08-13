using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>Đơn thuốc của buổi hẹn. Đơn thuốc luôn được đọc kèm danh mục thuốc (Items) vì
/// DTO trả về cho bác sĩ/bệnh nhân không bao giờ hiển thị đơn thuốc rỗng thông tin thuốc.</summary>
public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Mỗi buổi hẹn tối đa 1 đơn thuốc → trả về đơn duy nhất (null nếu chưa kê).</summary>
    Task<Prescription?> GetByAppointmentIdWithItemsAsync(Guid appointmentId, CancellationToken ct = default);

    Task AddAsync(Prescription prescription, CancellationToken ct = default);
    Task UpdateAsync(Prescription prescription, CancellationToken ct = default);
}
