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
        var act = () => _handler.HandleAsync(new GenerateMarketingContentRequest(null, null, null, null));

        await act.Should().ThrowAsync<ValidationException>();
        await _aiChatService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Chỉ nhập chủ đề tự do (không chọn dịch vụ/ưu đãi) vẫn phải hoạt động được.</summary>
    [Test]
    public async Task HandleAsync_TopicOnly_CallsAiWithTopicInPrompt()
    {
        var result = await _handler.HandleAsync(
            new GenerateMarketingContentRequest(null, null, "Chăm sóc răng cho trẻ em", null));

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

        await _handler.HandleAsync(new GenerateMarketingContentRequest(service.Id, null, null, null));

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p =>
                p.Contains("Tẩy trắng răng") &&
                p.Contains("60 phút") &&
                p.Contains("Tẩy trắng an toàn, hiệu quả nhanh")),
            "MarketingContent",
            Arg.Any<CancellationToken>());
    }
}
