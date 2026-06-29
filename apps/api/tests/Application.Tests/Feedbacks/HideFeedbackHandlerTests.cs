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
public class HideFeedbackHandlerTests
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
    /// Ẩn feedback hợp lệ phải chuyển trạng thái sang Hidden và gọi UpdateAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidFeedback_StatusBecomesHidden()
    {
        var feedback = Feedback.Create("Khách A", 5, "Tốt");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new HideFeedbackHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(feedback.Id);

        result.Status.Should().Be("Hidden");
        await _repo.Received(1).UpdateAsync(feedback, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ẩn feedback không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new HideFeedbackHandler(_repo, _activityLog, _currentUser);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
