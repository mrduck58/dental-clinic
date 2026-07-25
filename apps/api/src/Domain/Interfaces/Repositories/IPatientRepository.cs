using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Patient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task DeleteAsync(Patient patient, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Patient>> GetFamilyMembersAsync(Guid primaryPatientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tìm bệnh nhân theo họ tên hoặc số điện thoại (khớp một phần, không phân biệt hoa thường).
    /// Số điện thoại được dò cả ở <see cref="Patient.PhoneNumber"/> (bệnh nhân tạo tại quầy, không có
    /// tài khoản) lẫn ở tài khoản liên kết — hai nguồn này không phải lúc nào cũng trùng nhau.
    /// </summary>
    Task<IReadOnlyList<Patient>> SearchAsync(string term, int limit, CancellationToken cancellationToken = default);
}
