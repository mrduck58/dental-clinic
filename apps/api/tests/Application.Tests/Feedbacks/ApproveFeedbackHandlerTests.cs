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
}
