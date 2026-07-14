using DentalClinic.API.Application.UseCases.Posts;
using FluentAssertions;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class MarketingContentParserTests
{
    private static readonly string[] Categories =
    [
        "Chăm sóc răng miệng", "Niềng răng", "Phục hình", "Khuyến mãi", "Lời khuyên nha khoa",
    ];

    /// <summary>Đúng định dạng yêu cầu phải tách được tiêu đề/danh mục/nội dung chính xác.</summary>
    [Test]
    public void Parse_WellFormedResponse_ExtractsAllFields()
    {
        const string raw = """
            TIEU_DE: Ưu đãi tẩy trắng răng mùa hè
            DANH_MUC: Khuyến mãi
            ---
            Tẩy trắng răng chuyên nghiệp giúp bạn tự tin hơn mỗi ngày.
            Đặt lịch ngay hôm nay để nhận ưu đãi hấp dẫn!
            """;

        var result = MarketingContentParser.Parse(raw, Categories);

        result.Title.Should().Be("Ưu đãi tẩy trắng răng mùa hè");
        result.SuggestedCategory.Should().Be("Khuyến mãi");
        result.Content.Should().Contain("Tẩy trắng răng chuyên nghiệp");
    }

    /// <summary>AI trả sai chính tả danh mục (khác hoàn toàn) phải rơi về danh mục đầu tiên trong danh
    /// sách cho phép, không được giữ nguyên giá trị không hợp lệ (form sẽ không nhận danh mục lạ).</summary>
    [Test]
    public void Parse_UnknownCategory_FallsBackToFirstAllowedCategory()
    {
        const string raw = """
            TIEU_DE: Bài viết test
            DANH_MUC: Danh mục không tồn tại
            ---
            Nội dung bài viết.
            """;

        var result = MarketingContentParser.Parse(raw, Categories);

        result.SuggestedCategory.Should().Be("Chăm sóc răng miệng");
    }

    /// <summary>Danh mục AI trả khác hoa/thường so với danh sách cho phép vẫn phải khớp đúng
    /// (so sánh không phân biệt hoa/thường), giữ nguyên chính tả chuẩn trong danh sách.</summary>
    [Test]
    public void Parse_CategoryCaseInsensitiveMatch_NormalizesToAllowedSpelling()
    {
        const string raw = """
            TIEU_DE: Bài viết test
            DANH_MUC: khuyến mãi
            ---
            Nội dung.
            """;

        var result = MarketingContentParser.Parse(raw, Categories);

        result.SuggestedCategory.Should().Be("Khuyến mãi");
    }

    /// <summary>AI không theo đúng định dạng (thiếu "---") không được chặn người dùng — toàn bộ văn bản
    /// phải được coi là nội dung nháp để họ tự chỉnh sửa.</summary>
    [Test]
    public void Parse_MissingSeparator_TreatsWholeTextAsContent()
    {
        const string raw = "Đây chỉ là một đoạn văn bản tự do không theo định dạng yêu cầu.";

        var result = MarketingContentParser.Parse(raw, Categories);

        result.Content.Should().Be(raw);
        result.Title.Should().Be("Bài viết mới");
        result.SuggestedCategory.Should().Be("Chăm sóc răng miệng");
    }
}
