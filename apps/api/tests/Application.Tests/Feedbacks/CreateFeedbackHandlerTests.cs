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
public class CreateFeedbackHandlerTests
{
    private IFeedbackRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IFeedbackRepository>();

    /// <summary>
    /// Tạo feedback hợp lệ (rating 1-5) phải gọi AddAsync 1 lần và trả về DTO.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRating_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateFeedbackHandler(_repo);

        var result = await handler.HandleAsync(new CreateFeedbackRequest("Nguyễn Văn A", 5, "Rất tốt!"));

        await _repo.Received(1).AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
        result.CustomerName.Should().Be("Nguyễn Văn A");
        result.Rating.Should().Be(5);
    }

    /// <summary>
    /// Feedback mới tạo phải có trạng thái Pending mặc định.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewFeedback_StatusIsPending()
    {
        var handler = new CreateFeedbackHandler(_repo);

        var result = await handler.HandleAsync(new CreateFeedbackRequest("Khách Hàng", 4, "Ổn"));

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Rating dưới 1 phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingBelowOne_ThrowsValidationException()
    {
        var handler = new CreateFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(new CreateFeedbackRequest("A", 0, "Quá tệ"));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating trên 5 phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingAboveFive_ThrowsValidationException()
    {
        var handler = new CreateFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(new CreateFeedbackRequest("A", 6, "Xuất sắc"));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating ở biên dưới (=1) là hợp lệ, không được ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingBoundaryOne_IsValid()
    {
        var handler = new CreateFeedbackHandler(_repo);

        var result = await handler.HandleAsync(new CreateFeedbackRequest("A", 1, "Tệ"));

        result.Rating.Should().Be(1);
        await _repo.Received(1).AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating ở biên trên (=5) là hợp lệ, không được ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingBoundaryFive_IsValid()
    {
        var handler = new CreateFeedbackHandler(_repo);

        var result = await handler.HandleAsync(new CreateFeedbackRequest("A", 5, "Xuất sắc"));

        result.Rating.Should().Be(5);
        await _repo.Received(1).AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }
}
