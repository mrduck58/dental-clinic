using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Chat;

public class GetConversationMessagesHandler(IPatientRepository patientRepository, AppDbContext dbContext)
{
    public async Task<ConversationMessagesDto> HandleAsync(Guid userId, Guid conversationId, CancellationToken ct = default)
    {
        var patient = await patientRepository.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ bệnh nhân.");

        var conversation = await dbContext.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        // Trả 404 chung cho cả "không tồn tại" lẫn "thuộc bệnh nhân khác" — tránh lộ thông tin
        // về sự tồn tại của cuộc trò chuyện thuộc người khác.
        if (conversation is null || conversation.PatientId != patient.Id)
        {
            throw new NotFoundException("Không tìm thấy cuộc trò chuyện.");
        }

        var messages = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessageDto(m.Role, m.Content, m.CreatedAt))
            .ToList();

        return new ConversationMessagesDto(conversation.Id, messages);
    }
}
