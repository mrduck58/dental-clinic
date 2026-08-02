using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IDentistRepository
{
    /// <summary>Tìm nha sĩ khớp Id CỦA DENTIST hoặc Id CỦA USER liên kết (kèm User) — nhiều màn hình
    /// nhận cả hai loại Id từ client nên phải khớp cả hai cột.</summary>
    Task<Dentist?> GetByIdOrUserIdAsync(Guid idOrUserId, CancellationToken ct = default);

    /// <summary>Tìm nha sĩ theo Id tài khoản User, không kèm User (nơi gọi chỉ cần Id của Dentist).</summary>
    Task<Dentist?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Tìm nha sĩ theo Id tài khoản User, kèm User (nơi gọi cần FullName/thông tin tài khoản).</summary>
    Task<Dentist?> GetByUserIdWithUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Toàn bộ nha sĩ kèm User — dùng cho các nơi cần lọc theo tên/trạng thái ở tầng application.</summary>
    Task<List<Dentist>> GetAllWithUserAsync(CancellationToken ct = default);

    /// <summary>Projection lấy UserId từ DentistId, không tải cả entity.</summary>
    Task<Guid?> GetUserIdByDentistIdAsync(Guid dentistId, CancellationToken ct = default);

    Task AddAsync(Dentist dentist, CancellationToken ct = default);
}
