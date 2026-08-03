using DentalClinic.API.Application.UseCases.Posts;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Posts;

[TestFixture]
public class GenerateMarketingContentHandlerTests
{
    private IServiceRepository _serviceRepo = null!;
    private IPromotionRepository _promotionRepo = null!;
    private IAiChatService _aiChatService = null!;
    private GenerateMarketingContentHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _serviceRepo = Substitute.For<IServiceRepository>();
        _promotionRepo = Substitute.For<IPromotionRepository>();
        _aiChatService = Substitute.For<IAiChatService>();
        _aiChatService.SummarizeAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("TIEU_DE: Test\nDANH_MUC: Khuyến mãi\n---\nNội dung test.");

        _handler = new GenerateMarketingContentHandler(_serviceRepo, _promotionRepo, _aiChatService);
    }

    /// <summary>Không chọn dịch vụ/ưu đãi và không nhập chủ đề → không có gì để AI viết về,
    /// phải từ chối ngay mà không tốn một lệnh gọi AI.</summary>
    [Test]
    public async Task HandleAsync_NoServiceNoPromotionNoTopic_ThrowsValidationWithoutCallingAi()
    {
        var act = () => _handler.Handle(new GenerateMarketingContentRequest(null, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _aiChatService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Chỉ nhập chủ đề tự do (không chọn dịch vụ/ưu đãi) vẫn phải hoạt động được.</summary>
    [Test]
    public async Task HandleAsync_TopicOnly_CallsAiWithTopicInPrompt()
    {
        var result = await _handler.Handle(
            new GenerateMarketingContentRequest(null, null, "Chăm sóc răng cho trẻ em", null), CancellationToken.None);

        result.Title.Should().Be("Test");

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p => p.Contains("Chăm sóc răng cho trẻ em")),
            "MarketingContent",
            Arg.Any<CancellationToken>());
    }

    /// <summary>Dữ liệu dịch vụ thật (giá, mô tả) phải được đưa vào prompt gửi cho AI — đúng tinh thần
    /// "chỉ soạn dựa trên dữ liệu có sẵn, không bịa".</summary>
    [Test]
    public async Task HandleAsync_WithService_IncludesServiceDataInPrompt()
    {
        var service = Service.Create("Tẩy trắng răng", 1_500_000m, 60, "Tẩy trắng an toàn, hiệu quả nhanh");
        _serviceRepo.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);

        await _handler.Handle(new GenerateMarketingContentRequest(service.Id, null, null, null), CancellationToken.None);

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p =>
                p.Contains("Tẩy trắng răng") &&
                p.Contains("60 phút") &&
                p.Contains("Tẩy trắng an toàn, hiệu quả nhanh")),
            "MarketingContent",
            Arg.Any<CancellationToken>());
    }

    /// <summary>Ưu đãi giảm theo phần trăm (Percentage) phải hiển thị dạng "x%" trong prompt,
    /// kèm mã ưu đãi và ngày hết hạn, để AI không bịa thêm thông tin ưu đãi.</summary>
    [Test]
    public async Task HandleAsync_WithPercentagePromotion_IncludesPromotionDataInPrompt()
    {
        var promotion = Promotion.Create(
            "SALE20", "Ưu đãi hè", "Giảm giá dịp hè", "Percentage", 20m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            true);
        _promotionRepo.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        await _handler.Handle(new GenerateMarketingContentRequest(null, promotion.Id, null, null), CancellationToken.None);

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p =>
                p.Contains("Ưu đãi hè") &&
                p.Contains("SALE20") &&
                p.Contains("20%") &&
                p.Contains("Giảm giá dịp hè")),
            "MarketingContent",
            Arg.Any<CancellationToken>());
    }

    /// <summary>Ưu đãi giảm theo số tiền cố định (khác "Percentage") phải hiển thị dạng "xđ",
    /// không được nhầm sang định dạng phần trăm.</summary>
    [Test]
    public async Task HandleAsync_WithFixedAmountPromotion_FormatsDiscountAsCurrency()
    {
        var promotion = Promotion.Create(
            "SALE50K", "Ưu đãi giảm tiền mặt", null, "FixedAmount", 50_000m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            true);
        _promotionRepo.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        await _handler.Handle(new GenerateMarketingContentRequest(null, promotion.Id, null, null), CancellationToken.None);

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p => p.Contains("50.000đ") || p.Contains("50,000đ")),
            "MarketingContent",
            Arg.Any<CancellationToken>());
    }

    /// <summary>Chủ đề chỉ gồm khoảng trắng (không phải null) và không chọn dịch vụ/ưu đãi vẫn phải
    /// bị coi là "không có gì để viết" và từ chối, không tốn lệnh gọi AI.</summary>
    [Test]
    public async Task HandleAsync_WhitespaceOnlyTopic_ThrowsValidationWithoutCallingAi()
    {
        var act = () => _handler.Handle(new GenerateMarketingContentRequest(null, null, "   ", null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _aiChatService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>AI trả về chuỗi rỗng/toàn khoảng trắng (lỗi hoặc không sinh được nội dung) không được
    /// làm handler crash — phải rơi về nội dung nháp rỗng với tiêu đề/danh mục mặc định để nhân viên
    /// tự bổ sung, thay vì để lỗi lan ra ngoài.</summary>
    [Test]
    public async Task HandleAsync_AiReturnsEmptyResponse_ReturnsDraftWithDefaultTitle()
    {
        _aiChatService.SummarizeAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(string.Empty);

        var result = await _handler.Handle(
            new GenerateMarketingContentRequest(null, null, "Chủ đề bất kỳ", null), CancellationToken.None);

        result.Title.Should().Be("Bài viết mới");
        result.Content.Should().BeEmpty();
    }
}
