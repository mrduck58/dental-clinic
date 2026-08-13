using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Chat;

public record GetMyConversationsQuery(Guid UserId) : IRequest<IReadOnlyList<ChatConversationSummaryDto>>;

public class GetMyConversationsHandler(
    IPatientRepository patientRepository, IChatConversationRepository chatConversationRepository)
    : IRequestHandler<GetMyConversationsQuery, IReadOnlyList<ChatConversationSummaryDto>>
{
    public async Task<IReadOnlyList<ChatConversationSummaryDto>> Handle(GetMyConversationsQuery request, CancellationToken ct)
    {
        var patient = await patientRepository.GetByUserIdAsync(request.UserId, ct);
        if (patient is null) return [];

        var conversations = await chatConversationRepository.GetByPatientIdWithMessagesAsync(patient.Id, ct);

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
