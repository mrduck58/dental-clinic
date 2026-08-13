using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ChatConversationRepository(AppDbContext db) : IChatConversationRepository
{
    public async Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.ChatConversations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<ChatConversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default)
        => await db.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<ChatConversation>> GetByPatientIdWithMessagesAsync(
        Guid patientId, CancellationToken ct = default)
        => await db.ChatConversations
            .Include(c => c.Messages)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ChatConversation conversation, CancellationToken ct = default)
    {
        await db.ChatConversations.AddAsync(conversation, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> CountCreatedSinceAsync(DateTimeOffset since, CancellationToken ct = default)
        => await db.ChatConversations.CountAsync(c => c.CreatedAt >= since, ct);
}
