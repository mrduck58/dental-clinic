using System.Text;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;

namespace DentalClinic.API.Application.UseCases.Posts;

public record GenerateMarketingContentRequest(Guid? ServiceId, Guid? PromotionId, string? Topic, string? Tone);

/// <summary>
/// Soạn nháp bài viết marketing (tiêu đề + nội dung + danh mục gợi ý) từ dữ liệu dịch vụ/ưu đãi có sẵn
/// trong hệ thống, hỗ trợ nhân viên viết bài nhanh hơn. Chỉ tổng hợp từ dữ liệu thật — không bịa giá/ưu
/// đãi/tính năng không có trong DB. Kết quả LUÔN chỉ là bản NHÁP: nhân viên xem lại, chỉnh sửa và tự
/// bấm lưu/xuất bản như quy trình tạo bài viết thông thường — AI không tự đăng bài.
/// </summary>
public class GenerateMarketingContentHandler(
    IServiceRepository serviceRepository,
    IPromotionRepository promotionRepository,
    IAiChatService aiChatService)
{
    // Phải khớp danh sách danh mục cố định ở form tạo bài viết (apps/admin_website .../posts/create).
    private static readonly string[] AllowedCategories =
    [
        "Chăm sóc răng miệng", "Niềng răng", "Phục hình", "Khuyến mãi", "Lời khuyên nha khoa",
    ];

    public async Task<MarketingContentDraftDto> HandleAsync(
        GenerateMarketingContentRequest request, CancellationToken ct = default)
    {
        var service = request.ServiceId.HasValue
            ? await serviceRepository.GetByIdAsync(request.ServiceId.Value, ct)
            : null;
        var promotion = request.PromotionId.HasValue
            ? await promotionRepository.GetByIdAsync(request.PromotionId.Value, ct)
            : null;

        if (service is null && promotion is null && string.IsNullOrWhiteSpace(request.Topic))
        {
            throw new ValidationException(
                "Cần chọn dịch vụ, ưu đãi hoặc nhập chủ đề để AI soạn nội dung.");
        }

        var systemInstruction = BuildSystemInstruction(request.Tone);
        var prompt = BuildPrompt(service, promotion, request.Topic);

        var raw = await aiChatService.SummarizeAsync(
            systemInstruction, prompt, feature: "MarketingContent", ct: ct);

        return MarketingContentParser.Parse(raw, AllowedCategories);
    }

    private static string BuildSystemInstruction(string? tone)
    {
        var toneText = string.IsNullOrWhiteSpace(tone) ? "chuyên nghiệp, thân thiện" : tone;

        var sb = new StringBuilder();
        sb.AppendLine("Bạn là chuyên viên marketing của phòng khám nha khoa, soạn NHÁP bài viết quảng bá");
        sb.AppendLine("dựa HOÀN TOÀN vào dữ liệu được cung cấp bên dưới — nhân viên sẽ xem lại trước khi đăng.");
        sb.AppendLine($"Giọng văn: {toneText}. Bài viết dài khoảng 150-300 chữ, thu hút nhưng KHÔNG phóng đại");
        sb.AppendLine("hay cam kết sai sự thật về hiệu quả y khoa.");
        sb.AppendLine("TUYỆT ĐỐI KHÔNG bịa thêm giá, ưu đãi, hay tính năng không có trong dữ liệu bên dưới.");
        sb.AppendLine($"Danh mục PHẢI là ĐÚNG MỘT trong các giá trị sau (giữ nguyên chính tả): {string.Join(", ", AllowedCategories)}.");
        sb.AppendLine();
        sb.AppendLine("Trả lời ĐÚNG theo định dạng sau, không kèm markdown hay giải thích gì thêm:");
        sb.AppendLine("TIEU_DE: <tiêu đề bài viết>");
        sb.AppendLine("DANH_MUC: <một trong các danh mục trên>");
        sb.AppendLine("---");
        sb.AppendLine("<nội dung bài viết đầy đủ, có thể nhiều đoạn>");
        return sb.ToString();
    }

    private static string BuildPrompt(Service? service, Promotion? promotion, string? topic)
    {
        var sb = new StringBuilder();
        if (service is not null)
        {
            sb.AppendLine($"Dịch vụ: {service.Name}");
            sb.AppendLine($"Giá: {service.Price:N0}đ, thời gian khoảng {service.DurationMinutes} phút.");
            sb.AppendLine($"Mô tả: {service.Description}");
        }

        if (promotion is not null)
        {
            var discount = promotion.DiscountType == "Percentage"
                ? $"{promotion.DiscountValue}%"
                : $"{promotion.DiscountValue:N0}đ";
            sb.AppendLine($"Ưu đãi: {promotion.Name} (mã {promotion.Code}) — giảm {discount}, áp dụng đến {promotion.EndDate:dd/MM/yyyy}.");
            if (!string.IsNullOrWhiteSpace(promotion.Description))
            {
                sb.AppendLine($"Mô tả ưu đãi: {promotion.Description}");
            }
        }

        if (!string.IsNullOrWhiteSpace(topic))
        {
            sb.AppendLine($"Chủ đề/yêu cầu thêm từ nhân viên: {topic}");
        }

        return sb.ToString();
    }
}
