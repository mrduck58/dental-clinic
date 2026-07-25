using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class PostRepository(AppDbContext db) : IPostRepository
{
    public async Task<IEnumerable<Post>> GetAllAsync(CancellationToken ct = default)
        => await db.Posts
            .Include(p => p.Service)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Posts
            .Include(p => p.Service)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Post post, CancellationToken ct = default)
    {
        await db.Posts.AddAsync(post, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Post post, CancellationToken ct = default)
    {
        db.Posts.Update(post);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Post post, CancellationToken ct = default)
    {
        db.Posts.Remove(post);
        await db.SaveChangesAsync(ct);
    }
}
