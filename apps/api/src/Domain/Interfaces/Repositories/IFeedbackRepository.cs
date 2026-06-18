using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IFeedbackRepository
{
    Task<IEnumerable<Feedback>> GetAllAsync(CancellationToken ct = default);
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Feedback feedback, CancellationToken ct = default);
    Task UpdateAsync(Feedback feedback, CancellationToken ct = default);
}
