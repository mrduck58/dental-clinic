using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IMaterialRequestRepository
{
    Task AddAsync(MaterialRequest request, CancellationToken ct = default);
    Task<MaterialRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lọc động theo Status?, PatientId?, PatientName?.
    /// Nếu có cả PatientId và PatientName: khớp PatientId == PatientId HOẶC PatientName == PatientName.
    /// Kết quả sắp xếp OrderByDescending(CreatedAt), AsNoTracking.
    /// </summary>
    Task<IEnumerable<MaterialRequest>> SearchAsync(
        string? status,
        Guid? patientId,
        string? patientName,
        CancellationToken ct = default);

    Task UpdateAsync(MaterialRequest request, CancellationToken ct = default);

    /// <summary>
    /// Mở transaction thủ công khi cần gộp nhiều lệnh ghi (ví dụ nhiều SupplyItem/SupplyTransaction phát sinh
    /// từ việc nhập kho từng dòng vật tư) với UpdateAsync(request) thành 1 khối atomic. Trả về null nếu
    /// provider hiện tại không hỗ trợ transaction thật (ví dụ InMemory provider dùng trong unit test) — caller
    /// khi đó bỏ qua bước CommitAsync/DisposeAsync (vẫn có thể gọi DisposeAsync an toàn nếu muốn, nhưng nên
    /// kiểm tra null trước).
    /// </summary>
    Task<IMaterialRequestTransaction?> BeginTransactionAsync(CancellationToken ct = default);
}

/// <summary>Bọc transaction EF Core thành 1 abstraction nhỏ để Application layer không phải phụ thuộc trực
/// tiếp vào Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction.</summary>
public interface IMaterialRequestTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
