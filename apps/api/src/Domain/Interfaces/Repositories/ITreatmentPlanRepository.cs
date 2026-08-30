namespace DentalClinic.API.Domain.Interfaces.Repositories;

using DentalClinic.API.Domain.Entities;

public interface ITreatmentPlanRepository
{
    Task<TreatmentPlan?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TreatmentPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<TreatmentPlan?> GetByIdWithDentistAsync(Guid id, CancellationToken ct = default);
    Task<TreatmentPlanItem?> GetItemByIdAsync(Guid itemId, CancellationToken ct = default);
    Task<TreatmentPlanItem?> GetItemWithDetailsAsync(Guid itemId, CancellationToken ct = default);
    Task<TreatmentSession?> GetSessionByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<TreatmentPlan>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
    Task<IReadOnlyList<TreatmentPlan>> GetAllWithServiceAsync(CancellationToken ct = default);

    Task AddAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default);
    Task UpdateAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default);
    Task DeleteAsync(TreatmentPlan treatmentPlan, CancellationToken ct = default);

    Task AddItemAsync(TreatmentPlanItem item, CancellationToken ct = default);
    Task UpdateItemAsync(TreatmentPlanItem item, CancellationToken ct = default);
    Task DeleteItemAsync(TreatmentPlanItem item, CancellationToken ct = default);

    Task AddSessionAsync(TreatmentSession session, CancellationToken ct = default);
    Task UpdateSessionAsync(TreatmentSession session, CancellationToken ct = default);
    Task DeleteSessionAsync(TreatmentSession session, CancellationToken ct = default);

    Task<Dictionary<Guid, decimal>> GetPlanPaidMapAsync(List<Guid> planIds, CancellationToken ct = default);
    Task<Dictionary<Guid, decimal>> GetPlanBilledMapAsync(List<Guid> planIds, CancellationToken ct = default);
    Task<IReadOnlyList<ActiveTreatmentPlanSummary>> GetActiveByPatientIdsAsync(List<Guid> patientIds, CancellationToken ct = default);
}

public record ActiveTreatmentPlanSummary(Guid AppointmentId, Guid ServiceId, string ServiceName);
