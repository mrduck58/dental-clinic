using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IDentistRepository
{
    /// <summary>Tìm hồ sơ nha sĩ khớp Id CỦA DENTISTPROFILE hoặc Id CỦA USER liên kết (kèm Employee+User) —
    /// nhiều màn hình nhận cả hai loại Id từ client nên phải khớp cả hai cột.</summary>
    Task<DentistProfile?> GetByIdOrUserIdAsync(Guid idOrUserId, CancellationToken ct = default);

    /// <summary>Tìm hồ sơ nha sĩ theo Id tài khoản User, không kèm User (nơi gọi chỉ cần Id của DentistProfile).</summary>
    Task<DentistProfile?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Tìm hồ sơ nha sĩ theo Id tài khoản User, kèm Employee+User (nơi gọi cần FullName/thông tin tài khoản).</summary>
    Task<DentistProfile?> GetByUserIdWithUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Toàn bộ hồ sơ nha sĩ kèm Employee+User — dùng cho các nơi cần lọc theo tên/trạng thái ở tầng application.</summary>
    Task<List<DentistProfile>> GetAllWithUserAsync(CancellationToken ct = default);

    /// <summary>Projection lấy UserId từ DentistProfileId, không tải cả entity.</summary>
    Task<Guid?> GetUserIdByDentistIdAsync(Guid dentistProfileId, CancellationToken ct = default);

    Task AddAsync(DentistProfile dentistProfile, CancellationToken ct = default);
    Task UpdateAsync(DentistProfile dentistProfile, CancellationToken ct = default);

    /// <summary>Toàn bộ hồ sơ nha sĩ có tài khoản Active, kèm Employee+User.</summary>
    Task<List<DentistProfile>> GetAllActiveWithUserAsync(CancellationToken ct = default);

    /// <summary>Hồ sơ nha sĩ có tài khoản Active VÀ tên khớp một trong các tên được phân ca — dùng khi lọc theo WorkSchedule.StaffName.</summary>
    Task<List<DentistProfile>> GetActiveByNamesAsync(IEnumerable<string> names, CancellationToken ct = default);
}
