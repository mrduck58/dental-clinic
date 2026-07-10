using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Chat;

public class GetMyConversationsHandler(IPatientRepository patientRepository, AppDbContext dbContext)
{
    public async Task<IReadOnlyList<ChatConversationSummaryDto>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var patient = await patientRepository.GetByUserIdAsync(userId, ct);
        if (patient is null) return [];

        var conversations = await dbContext.ChatConversations
            .Include(c => c.Messages)
            .Where(c => c.PatientId == patient.Id)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

        return conversations
            .Select(c =>
            {
                var lastUserMessage = c.Messages
                    .Where(m => m.Role == "user")
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();
                var preview = lastUserMessage?.Content ?? "Cuộc trò chuyện mới";
                return new ChatConversationSummaryDto(c.Id, Truncate(preview, 80), c.UpdatedAt);
            })
            .ToList();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
