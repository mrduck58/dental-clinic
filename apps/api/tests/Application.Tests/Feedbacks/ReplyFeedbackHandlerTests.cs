using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class ReplyFeedbackHandlerTests
{
    private IFeedbackRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IFeedbackRepository>();

    /// <summary>
    /// Trả lời feedback hợp lệ phải lưu ReplyText và đặt trạng thái Featured.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidText_SetsReplyAndStatusFeatured()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ReplyFeedbackHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id, new ReplyFeedbackRequest("Cảm ơn bạn!"));

        result.ReplyText.Should().Be("Cảm ơn bạn!");
        result.Status.Should().Be("Featured");
    }

    /// <summary>
    /// Trả lời feedback đang bị Hidden phải giữ trạng thái Hidden,
    /// không tự động chuyển sang Featured.
    /// </summary>
    [Test]
    public async Task HandleAsync_HiddenFeedback_StatusRemainsHidden()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        feedback.Hide();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ReplyFeedbackHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id, new ReplyFeedbackRequest("Xin lỗi"));

        result.Status.Should().Be("Hidden");
        result.ReplyText.Should().Be("Xin lỗi");
    }

    /// <summary>
    /// Nội dung trả lời trống phải ném ValidationException trước khi tìm feedback.
    /// </summary>
    [Test]
    public async Task HandleAsync_EmptyText_ThrowsValidationException()
    {
        var handler = new ReplyFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new ReplyFeedbackRequest("   "));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Trả lời feedback không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new ReplyFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new ReplyFeedbackRequest("Reply"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Nội dung trả lời null (không chỉ whitespace) cũng phải ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NullText_ThrowsValidationException()
    {
        var handler = new ReplyFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new ReplyFeedbackRequest(null!));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Trả lời hợp lệ phải gọi UpdateAsync đúng 1 lần để lưu thay đổi.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidText_CallsUpdateAsyncOnce()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ReplyFeedbackHandler(_repo);

        await handler.HandleAsync(feedback.Id, new ReplyFeedbackRequest("Cảm ơn bạn!"));

        await _repo.Received(1).UpdateAsync(feedback, Arg.Any<CancellationToken>());
    }
}
