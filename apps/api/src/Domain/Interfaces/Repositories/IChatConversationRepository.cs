using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IChatConversationRepository
{
    /// <summary>Không kèm Messages — dùng khi chỉ cần xác thực chủ sở hữu/tồn tại của hội thoại.</summary>
    Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Kèm Messages — dùng khi cần đọc lại toàn bộ nội dung hội thoại.</summary>
    Task<ChatConversation?> GetByIdWithMessagesAsync(Guid id, CancellationToken ct = default);

    /// <summary>Tất cả hội thoại của một bệnh nhân, kèm Messages, hội thoại cập nhật gần nhất lên trước.</summary>
    Task<IReadOnlyList<ChatConversation>> GetByPatientIdWithMessagesAsync(
        Guid patientId, CancellationToken ct = default);

    Task AddAsync(ChatConversation conversation, CancellationToken ct = default);

    Task<int> CountCreatedSinceAsync(DateTimeOffset since, CancellationToken ct = default);
}
