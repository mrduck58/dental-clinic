using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IMaterialRequestRepository
{
    Task AddAsync(MaterialRequest request, CancellationToken ct = default);
    Task<MaterialRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Lọc động theo Status?, PatientId?/CourseId?, PatientName?.
    /// Nếu có cả PatientId và PatientName: khớp CourseId == PatientId HOẶC PatientName == PatientName.
    /// Kết quả sắp xếp OrderByDescending(CreatedAt), AsNoTracking.
    /// </summary>
    Task<IEnumerable<MaterialRequest>> SearchAsync(
        string? status,
        Guid? patientId,
        string? patientName,
        CancellationToken ct = default);

    Task UpdateAsync(MaterialRequest request, CancellationToken ct = default);
}
