using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

/// <summary>Phiếu khám (chuẩn đoán) của một buổi hẹn — chỉ phục vụ CRUD theo Id;
/// các luồng đọc tổng hợp nhiều bảng qua Appointment vẫn dùng AppDbContext.</summary>
public interface IDiagnosisRepository
{
    Task<Diagnosis?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Diagnosis diagnosis, CancellationToken ct = default);
    Task UpdateAsync(Diagnosis diagnosis, CancellationToken ct = default);
    Task DeleteAsync(Diagnosis diagnosis, CancellationToken ct = default);
}
