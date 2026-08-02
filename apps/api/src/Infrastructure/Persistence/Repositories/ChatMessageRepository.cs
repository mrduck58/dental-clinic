using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Infrastructure.Persistence.Repositories;

public class ChatMessageRepository(AppDbContext db) : IChatMessageRepository
{
    public async Task AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        await db.ChatMessages.AddAsync(message, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentByConversationAsync(
        Guid conversationId, int count, CancellationToken ct = default)
    {
        var messages = await db.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .ToListAsync(ct);
        messages.Reverse();
        return messages;
    }

    public async Task<int> CountRecentUserMessagesByPatientAsync(
        Guid patientId, DateTimeOffset since, CancellationToken ct = default)
        => await db.ChatMessages.CountAsync(
            m => m.Role == "user" && m.CreatedAt >= since &&
                 db.ChatConversations.Any(c => c.Id == m.ConversationId && c.PatientId == patientId),
            ct);

    public async Task<int> CountByRoleSinceAsync(string role, DateTimeOffset since, CancellationToken ct = default)
        => await db.ChatMessages.CountAsync(m => m.CreatedAt >= since && m.Role == role, ct);

    public async Task<IReadOnlyList<ChatMessage>> GetAssistantMessagesSinceAsync(
        DateTimeOffset since, CancellationToken ct = default)
        => await db.ChatMessages
            .Where(m => m.CreatedAt >= since && m.Role == "assistant")
            .ToListAsync(ct);
}
