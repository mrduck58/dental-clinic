using System.Text;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Feedbacks;

public record FeedbackReplyDraftDto(string ReplyText);

public record GenerateFeedbackReplyQuery(Guid Id) : IRequest<FeedbackReplyDraftDto>;

/// <summary>
/// Soạn NHÁP câu trả lời cho một đánh giá của khách hàng, hỗ trợ nhân viên/chủ phòng khám phản hồi
/// nhanh hơn — nhân viên xem lại, chỉnh sửa rồi tự gửi qua <c>ReplyFeedbackHandler</c> như quy trình
/// trả lời thông thường; AI không tự gửi phản hồi.
/// </summary>
public class GenerateFeedbackReplyHandler(IFeedbackRepository feedbackRepository, IAiChatService aiChatService)
    : IRequestHandler<GenerateFeedbackReplyQuery, FeedbackReplyDraftDto>
{
    public async Task<FeedbackReplyDraftDto> Handle(GenerateFeedbackReplyQuery request, CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy phản hồi với ID: {request.Id}");

        var raw = await aiChatService.SummarizeAsync(
            BuildSystemInstruction(), BuildPrompt(feedback), feature: "FeedbackReply", ct: cancellationToken);

        return new FeedbackReplyDraftDto(raw.Trim());
    }

    private static string BuildSystemInstruction()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là nhân viên chăm sóc khách hàng của phòng khám nha khoa, soạn NHÁP một câu trả");
        sb.AppendLine("lời cho đánh giá của khách hàng — nhân viên sẽ xem lại trước khi gửi.");
        sb.AppendLine("Giọng văn: lịch sự, chuyên nghiệp, chân thành.");
        sb.AppendLine("- Nếu đánh giá TÍCH CỰC (từ 4 sao trở lên): cảm ơn ngắn gọn, mời khách quay lại.");
        sb.AppendLine("- Nếu đánh giá TIÊU CỰC (từ 3 sao trở xuống): xin lỗi chân thành, thể hiện sự cầu thị,");
        sb.AppendLine("  mời khách liên hệ trực tiếp phòng khám để được hỗ trợ giải quyết. TUYỆT ĐỐI KHÔNG hứa");
        sb.AppendLine("  hẹn bồi thường/hoàn tiền cụ thể, KHÔNG đổ lỗi hay tranh cãi với khách hàng.");
        sb.AppendLine("Trả lời ngắn gọn (2-4 câu), không dùng markdown, ký tên chung là 'Đội ngũ phòng khám'.");
        sb.AppendLine("Chỉ trả về đúng nội dung câu trả lời, không kèm giải thích hay tiêu đề gì khác.");
        return sb.ToString();
    }

    private static string BuildPrompt(Feedback feedback)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Khách hàng: {feedback.CustomerName}");
        sb.AppendLine($"Đánh giá: {feedback.Rating}/5 sao");
        sb.AppendLine($"Nội dung: {feedback.Comment}");
        return sb.ToString();
    }
}
