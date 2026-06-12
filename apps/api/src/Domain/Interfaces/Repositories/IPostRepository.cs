using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default);
    Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Post post, CancellationToken ct = default);
    Task UpdateAsync(Post post, CancellationToken ct = default);
    Task DeleteAsync(Post post, CancellationToken ct = default);
}
