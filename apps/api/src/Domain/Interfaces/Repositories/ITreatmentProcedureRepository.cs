using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface ITreatmentProcedureRepository
{
    Task<IEnumerable<TreatmentProcedure>> GetByServiceIdAsync(Guid serviceId, CancellationToken ct = default);
    Task ReplaceAllForServiceAsync(Guid serviceId, IEnumerable<TreatmentProcedure> newProcedures, CancellationToken ct = default);
}
