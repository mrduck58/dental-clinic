using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class GetFeedbackByIdHandlerTests
{
    private IFeedbackRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IFeedbackRepository>();

    /// <summary>
    /// Lấy feedback theo ID hợp lệ phải trả về DTO với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task HandleAsync_ExistingFeedback_ReturnsDto()
    {
        var feedback = Feedback.Create("Nguyễn Test", 5, "Tốt");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new GetFeedbackByIdHandler(_repo);

        var result = await handler.Handle(new GetFeedbackByIdQuery(feedback.Id), CancellationToken.None);

        result.Id.Should().Be(feedback.Id);
        result.CustomerName.Should().Be("Nguyễn Test");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task HandleAsync_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new GetFeedbackByIdHandler(_repo);

        Func<Task> act = () => handler.Handle(new GetFeedbackByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
