using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class ApproveFeedbackHandlerTests
{
    private IFeedbackRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IFeedbackRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    /// <summary>
    /// Duyệt feedback ở trạng thái Pending phải chuyển sang Featured.
    /// </summary>
    [Test]
    public async Task HandleAsync_PendingFeedback_StatusBecomesFeatured()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(feedback.Id);

        result.Status.Should().Be("Featured");
    }

    /// <summary>
    /// Duyệt feedback đã Featured phải toggle về Pending (bỏ nổi bật).
    /// </summary>
    [Test]
    public async Task HandleAsync_FeaturedFeedback_StatusBecomesPending()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        feedback.Feature();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(feedback.Id);

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Duyệt feedback không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new ApproveFeedbackHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Sau khi toggle, phải gọi UpdateAsync đúng 1 lần để lưu thay đổi.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidFeedback_CallsUpdateAsyncOnce()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(feedback.Id);

        await _repo.Received(1).UpdateAsync(feedback, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Duyệt feedback đang bị Hidden phải chuyển sang Featured (nhánh else của điều kiện toggle),
    /// ghi đè trạng thái Hidden trước đó.
    /// </summary>
    [Test]
    public async Task HandleAsync_HiddenFeedback_StatusBecomesFeatured()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        feedback.Hide();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(feedback.Id);

        result.Status.Should().Be("Featured");
    }

    /// <summary>
    /// Duyệt/bỏ duyệt phải ghi activity log với đúng action Approve và module Feedback.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidFeedback_LogsActivityWithApproveAction()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo, _activityLog, _currentUser);

        await handler.HandleAsync(feedback.Id);

        await _activityLog.Received(1).LogAsync(
            userId: Arg.Any<Guid?>(),
            userName: Arg.Any<string>(),
            userRole: Arg.Any<string>(),
            action: ActivityAction.Approve,
            module: ActivityModule.Feedback,
            description: Arg.Any<string>(),
            status: Arg.Any<string>(),
            ipAddress: Arg.Any<string?>(),
            targetId: feedback.Id.ToString(),
            ct: Arg.Any<CancellationToken>());
    }
}
