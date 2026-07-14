namespace DentalClinic.API.Application.UseCases.Posts;

public record MarketingContentDraftDto(string Title, string Content, string SuggestedCategory);

/// <summary>
/// Parse văn bản tự do mà Gemini trả về (theo định dạng "TIEU_DE: ...\nDANH_MUC: ...\n---\n&lt;nội dung&gt;"
/// đã yêu cầu trong system instruction) thành tiêu đề/nội dung/danh mục cho form tạo bài viết. Tách
/// riêng khỏi <see cref="GenerateMarketingContentHandler"/> để test được logic parse mà không cần gọi AI thật.
/// </summary>
public static class MarketingContentParser
{
    public static MarketingContentDraftDto Parse(string raw, IReadOnlyList<string> allowedCategories)
    {
        var separatorIndex = raw.IndexOf("---", StringComparison.Ordinal);
        var header = separatorIndex >= 0 ? raw[..separatorIndex] : string.Empty;
        var body = separatorIndex >= 0 ? raw[(separatorIndex + 3)..].Trim() : string.Empty;

        var title = ExtractLine(header, "TIEU_DE:");
        var rawCategory = ExtractLine(header, "DANH_MUC:");

        // AI không theo đúng định dạng yêu cầu (thiếu "---" hoặc thiếu nhãn) — không chặn người dùng,
        // coi toàn bộ văn bản là nội dung nháp để họ tự chỉnh sửa tiêu đề/danh mục trước khi lưu.
        if (string.IsNullOrWhiteSpace(body))
        {
            body = raw.Trim();
        }

        var category = ResolveCategory(rawCategory, allowedCategories);

        return new MarketingContentDraftDto(
            string.IsNullOrWhiteSpace(title) ? "Bài viết mới" : title,
            body,
            category);
    }

    private static string ResolveCategory(string? rawCategory, IReadOnlyList<string> allowedCategories)
    {
        if (allowedCategories.Count == 0)
        {
            return rawCategory ?? string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(rawCategory))
        {
            var match = allowedCategories.FirstOrDefault(
                c => string.Equals(c, rawCategory, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return allowedCategories[0];
    }

    private static string? ExtractLine(string block, string prefix)
    {
        foreach (var line in block.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[prefix.Length..].Trim();
            }
        }
        return null;
    }
}
