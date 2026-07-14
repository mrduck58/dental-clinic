using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class GenerateFeedbackReplyHandlerTests
{
    private IFeedbackRepository _feedbackRepo = null!;
    private IAiChatService _aiChatService = null!;
    private GenerateFeedbackReplyHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _feedbackRepo = Substitute.For<IFeedbackRepository>();
        _aiChatService = Substitute.For<IAiChatService>();
        _aiChatService.SummarizeAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Cảm ơn bạn đã tin tưởng phòng khám. Đội ngũ phòng khám.");

        _handler = new GenerateFeedbackReplyHandler(_feedbackRepo, _aiChatService);
    }

    /// <summary>Không tìm thấy feedback → ném NotFoundException, KHÔNG gọi AI.</summary>
    [Test]
    public async Task HandleAsync_FeedbackNotFound_ThrowsNotFoundExceptionWithoutCallingAi()
    {
        _feedbackRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);

        var act = () => _handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        await _aiChatService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Dữ liệu đánh giá thật (tên khách, số sao, nội dung) phải được đưa vào prompt gửi AI,
    /// và phải gọi đúng feature "FeedbackReply" để usage log phân biệt được với các tính năng AI khác.</summary>
    [Test]
    public async Task HandleAsync_ValidFeedback_IncludesFeedbackDataInPromptWithCorrectFeature()
    {
        var feedback = Feedback.Create("Nguyễn Văn B", 2, "Chờ đợi quá lâu, nhân viên không nhiệt tình.");
        _feedbackRepo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);

        var result = await _handler.HandleAsync(feedback.Id);

        result.ReplyText.Should().Contain("Cảm ơn");

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p =>
                p.Contains("Nguyễn Văn B") &&
                p.Contains("2/5") &&
                p.Contains("Chờ đợi quá lâu")),
            "FeedbackReply",
            Arg.Any<CancellationToken>());
    }
}
