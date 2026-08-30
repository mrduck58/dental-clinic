namespace DentalClinic.API.Domain.Interfaces.Repositories;

using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;

public interface IFollowUpRepository
{
    Task<FollowUp?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FollowUp?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FollowUp>> GetByPatientIdAsync(Guid patientId, CancellationToken ct = default);
    Task<IReadOnlyList<FollowUp>> GetDueFollowUpsAsync(DateOnly? toDate = null, CancellationToken ct = default);
    Task<IReadOnlyList<FollowUp>> GetPendingByPatientIdAsync(Guid patientId, CancellationToken ct = default);
    Task AddAsync(FollowUp followUp, CancellationToken ct = default);
    Task UpdateAsync(FollowUp followUp, CancellationToken ct = default);
    Task DeleteAsync(FollowUp followUp, CancellationToken ct = default);
}
