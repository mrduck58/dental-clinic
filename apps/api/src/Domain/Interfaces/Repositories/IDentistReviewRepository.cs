using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IDentistReviewRepository
{
    /// <summary>Toàn bộ đánh giá của một nha sĩ, kèm Patient, mới nhất trước — dùng để hiện danh sách
    /// và tính điểm trung bình.</summary>
    Task<List<DentistReview>> GetByDentistIdAsync(Guid dentistId, CancellationToken ct = default);

    /// <summary>Đánh giá đã có của MỘT bệnh nhân cho MỘT nha sĩ (nếu có) — mỗi cặp chỉ có tối đa 1
    /// bản ghi, gửi lại sẽ cập nhật thay vì tạo mới.</summary>
    Task<DentistReview?> GetByDentistAndPatientAsync(Guid dentistId, Guid patientId, CancellationToken ct = default);

    /// <summary>Chỉ lấy điểm số các đánh giá của một nha sĩ — dùng để tính điểm trung bình mà không tải cả entity.</summary>
    Task<IReadOnlyList<int>> GetRatingsByDentistIdAsync(Guid dentistId, CancellationToken ct = default);

    Task AddAsync(DentistReview review, CancellationToken ct = default);
    Task UpdateAsync(DentistReview review, CancellationToken ct = default);
}
