using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ITreatmentProcedureRepository
{
    Task<IEnumerable<TreatmentProcedure>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);

    /// <summary>Quy trình của nhiều dịch vụ trong một lượt truy vấn — dùng khi tính tiến độ cho cả danh sách liệu trình.</summary>
    Task<IReadOnlyList<TreatmentProcedure>> GetByServiceIdsAsync(List<Guid> serviceIds, CancellationToken ct = default);

    Task ReplaceAllForServiceAsync(Guid serviceId, IEnumerable<TreatmentProcedure> newProcedures, CancellationToken ct = default);
}
