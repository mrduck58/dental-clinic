using DentalClinic.API.Domain.Entities;

namespace DentalClinic.API.Domain.Interfaces.Repositories;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken ct = default);

    /// <summary><paramref name="count"/> tin nhắn gần nhất của một hội thoại, trả về theo thứ tự thời
    /// gian TĂNG DẦN (cũ → mới) — dùng làm lịch sử ngữ cảnh cho AI.</summary>
    Task<IReadOnlyList<ChatMessage>> GetRecentByConversationAsync(
        Guid conversationId, int count, CancellationToken ct = default);

    /// <summary>Đếm tin nhắn "user" tính từ <paramref name="since"/> thuộc các hội thoại của một bệnh
    /// nhân cụ thể (qua mọi hội thoại) — dùng để chặn spam gọi AI theo bệnh nhân.</summary>
    Task<int> CountRecentUserMessagesByPatientAsync(
        Guid patientId, DateTimeOffset since, CancellationToken ct = default);

    Task<int> CountByRoleSinceAsync(string role, DateTimeOffset since, CancellationToken ct = default);

    Task<IReadOnlyList<ChatMessage>> GetAssistantMessagesSinceAsync(
        DateTimeOffset since, CancellationToken ct = default);
}
