using System.Text;
using DentalClinic.API.Application.UseCases.Staff;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Chat;

public class SendChatMessageHandler(
    IPatientRepository patientRepository,
    IClinicInfoRepository clinicInfoRepository,
    GetDentistsHandler getDentistsHandler,
    IAiChatService aiChatService,
    AppDbContext dbContext)
{
    public async Task<SendChatMessageResult> HandleAsync(
        Guid userId, Guid conversationId, string message, CancellationToken ct = default)
    {
        var patient = await patientRepository.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("Không tìm thấy hồ sơ bệnh nhân.");

        var conversation = await dbContext.ChatConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);

        if (conversation is null || conversation.PatientId != patient.Id)
        {
            throw new NotFoundException("Không tìm thấy cuộc trò chuyện.");
        }

        var snapshot = await BuildSnapshotAsync(ct);
        var systemInstruction = BuildSystemInstruction(snapshot, conversation);

        dbContext.ChatMessages.Add(ChatMessage.Create(conversation.Id, "user", message));

        var reply = await aiChatService.AskAsync(systemInstruction, message, ct);

        dbContext.ChatMessages.Add(ChatMessage.Create(conversation.Id, "assistant", reply.Reply));
        conversation.Touch();

        await dbContext.SaveChangesAsync(ct);

        var bookingHint = ResolveBookingHint(reply, snapshot);
        return new SendChatMessageResult(reply.Reply, reply.SuggestBooking, bookingHint);
    }

    private sealed record ClinicSnapshot(
        Domain.Entities.ClinicInfo? ClinicInfo,
        List<Service> Services,
        List<Promotion> Promotions,
        IEnumerable<DentistSummaryDto> Dentists,
        List<Post> Posts,
        DateOnly Today);

    private async Task<ClinicSnapshot> BuildSnapshotAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var clinicInfo = await clinicInfoRepository.GetAsync(ct);

        var services = await dbContext.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        // GetPromotionsHandler hiện không lọc theo IsActive/khoảng ngày nên không dùng lại được —
        // tự lọc trực tiếp ở đây để chatbot không nhắc tới ưu đãi đã hết hạn hoặc bị tắt.
        var promotions = await dbContext.Promotions
            .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today)
            .OrderBy(p => p.EndDate)
            .ToListAsync(ct);

        var dentists = await getDentistsHandler.HandleAsync(ct);

        var posts = await dbContext.Posts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedAt)
            .Take(5)
            .ToListAsync(ct);

        return new ClinicSnapshot(clinicInfo, services, promotions, dentists, posts, today);
    }

    /// <summary>
    /// Xây dựng system instruction cho Gemini từ snapshot dữ liệu phòng khám hiện tại
    /// (ClinicInfo + dịch vụ/ưu đãi đang active + bác sĩ + tin tức gần đây) cộng thêm guardrail.
    /// </summary>
    private static string BuildSystemInstruction(ClinicSnapshot snapshot, ChatConversation conversation)
    {
        var (clinicInfo, services, promotions, dentists, posts, today) = snapshot;

        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý AI của phòng khám nha khoa, trả lời bằng tiếng Việt, thân thiện, ngắn gọn, dễ hiểu.");
        sb.AppendLine($"Hôm nay là ngày {today:dd/MM/yyyy}.");
        sb.AppendLine("CHỈ trả lời các câu hỏi về thông tin phòng khám: dịch vụ, giá, giờ làm việc, ưu đãi, bác sĩ, tin tức, thông tin liên hệ.");
        sb.AppendLine("TUYỆT ĐỐI KHÔNG chẩn đoán bệnh, KHÔNG tư vấn điều trị y khoa cụ thể.");
        sb.AppendLine("Nếu bệnh nhân mô tả triệu chứng (đau, sưng, ê buốt, chảy máu...) hoặc hỏi về tình trạng răng miệng cụ thể của họ, hãy trả lời ngắn gọn rằng cần bác sĩ thăm khám trực tiếp, và đặt suggestBooking = true.");
        sb.AppendLine("Nếu bệnh nhân có ý muốn đặt lịch hẹn (kể cả khi họ chỉ nói mơ hồ như 'tôi muốn đặt lịch'), cũng đặt suggestBooking = true.");
        sb.AppendLine("Khi suggestBooking = true, hãy trích xuất vào bookingHint những gì bệnh nhân đã nói rõ (dịch vụ muốn dùng, tên bác sĩ muốn khám, ngày muốn đặt — quy đổi các cách nói tương đối như 'ngày mai', 'thứ 7 này' sang định dạng yyyy-MM-dd dựa trên hôm nay ở trên, và mô tả triệu chứng/ghi chú nếu có). Trường nào bệnh nhân không nói tới thì để null, KHÔNG tự suy đoán hoặc bịa ra.");
        sb.AppendLine("Nếu câu hỏi không liên quan đến phòng khám, hãy lịch sự từ chối và hướng người dùng quay lại các chủ đề trên.");
        sb.AppendLine("Chỉ trả lời dựa trên dữ liệu được cung cấp bên dưới — không bịa thêm thông tin không có trong dữ liệu.");
        sb.AppendLine();
        sb.AppendLine("Bạn PHẢI trả lời bằng đúng một object JSON hợp lệ, không kèm markdown hay giải thích gì thêm, theo đúng cấu trúc sau:");
        sb.AppendLine("""{"reply": "<câu trả lời bằng tiếng Việt>", "suggestBooking": <true hoặc false>, "bookingHint": {"serviceName": <string hoặc null>, "dentistName": <string hoặc null>, "preferredDate": <"yyyy-MM-dd" hoặc null>, "notes": <string hoặc null>}}""");
        sb.AppendLine();

        if (clinicInfo is not null)
        {
            sb.AppendLine("== Thông tin phòng khám ==");
            sb.AppendLine($"Địa chỉ: {clinicInfo.Address}");
            sb.AppendLine($"Điện thoại: {clinicInfo.Phone}");
            sb.AppendLine($"Email: {clinicInfo.Email}");
            if (!string.IsNullOrWhiteSpace(clinicInfo.WorkingHours))
            {
                sb.AppendLine($"Giờ làm việc: {clinicInfo.WorkingHours}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("== Dịch vụ đang cung cấp ==");
        foreach (var s in services)
        {
            sb.AppendLine($"- {s.Name}: {s.Price:N0}đ, thời gian khoảng {s.DurationMinutes} phút. {s.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("== Ưu đãi đang áp dụng ==");
        if (promotions.Count == 0)
        {
            sb.AppendLine("(Hiện không có ưu đãi nào đang áp dụng.)");
        }
        foreach (var p in promotions)
        {
            var discount = p.DiscountType == "Percentage" ? $"{p.DiscountValue}%" : $"{p.DiscountValue:N0}đ";
            sb.AppendLine($"- {p.Name} ({p.Code}): giảm {discount}, áp dụng đến {p.EndDate:dd/MM/yyyy}. {p.Description}");
        }
        sb.AppendLine();

        sb.AppendLine("== Đội ngũ bác sĩ ==");
        foreach (var d in dentists)
        {
            sb.AppendLine($"- {d.FullName}, chuyên khoa: {d.Specialty}, {d.YearsOfExperience} năm kinh nghiệm.");
        }
        sb.AppendLine();

        if (posts.Count > 0)
        {
            sb.AppendLine("== Tin tức / bài viết gần đây ==");
            foreach (var post in posts)
            {
                sb.AppendLine($"- {post.Title} ({post.Category})");
            }
            sb.AppendLine();
        }

        var recentHistory = conversation.Messages
            .OrderBy(m => m.CreatedAt)
            .TakeLast(10)
            .Select(m => $"{(m.Role == "user" ? "Bệnh nhân" : "Bot")}: {m.Content}")
            .ToList();

        if (recentHistory.Count > 0)
        {
            sb.AppendLine("== Lịch sử hội thoại gần đây ==");
            foreach (var line in recentHistory)
            {
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Đối chiếu tên dịch vụ/bác sĩ AI trích xuất (dạng tự nhiên, có thể không khớp tuyệt đối) với dữ liệu
    /// thật đã dùng để build snapshot — chỉ trả về Id khi tìm được một kết quả khớp rõ ràng, tránh mobile
    /// điền sai dữ liệu vào form đặt lịch từ một cái tên AI đoán/viết sai.
    /// </summary>
    private static BookingHintDto ResolveBookingHint(AiChatReply reply, ClinicSnapshot snapshot)
    {
        Guid? serviceId = null;
        string? serviceName = null;
        if (!string.IsNullOrWhiteSpace(reply.ServiceNameHint))
        {
            var match = snapshot.Services.FirstOrDefault(s =>
                s.Name.Contains(reply.ServiceNameHint, StringComparison.OrdinalIgnoreCase) ||
                reply.ServiceNameHint.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                serviceId = match.Id;
                serviceName = match.Name;
            }
        }

        Guid? dentistId = null;
        string? dentistName = null;
        if (!string.IsNullOrWhiteSpace(reply.DentistNameHint))
        {
            var match = snapshot.Dentists.FirstOrDefault(d =>
                !string.IsNullOrWhiteSpace(d.FullName) &&
                (d.FullName.Contains(reply.DentistNameHint, StringComparison.OrdinalIgnoreCase) ||
                 reply.DentistNameHint.Contains(d.FullName, StringComparison.OrdinalIgnoreCase)));
            if (match is not null)
            {
                dentistId = match.Id;
                dentistName = match.FullName;
            }
        }

        // Bỏ qua nếu AI lỡ tính ra một ngày đã qua — mobile không thể chọn ngày quá khứ trong lịch.
        var preferredDate = reply.PreferredDate is { } d2 && d2 >= snapshot.Today ? (DateOnly?)d2 : null;

        return new BookingHintDto(serviceId, serviceName, dentistId, dentistName, preferredDate, reply.NotesHint);
    }
}
