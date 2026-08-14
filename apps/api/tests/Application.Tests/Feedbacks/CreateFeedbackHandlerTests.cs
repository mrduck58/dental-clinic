using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Application.UseCases.Dentists;
using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class CreateFeedbackHandlerTests
{
    private IFeedbackRepository _feedbackRepo = null!;
    private IPatientRepository _patientRepo = null!;
    private INotificationService _notification = null!;
    private IUserRepository _userRepo = null!;
    private Patient _patient = null!;

    [SetUp]
    public void SetUp()
    {
        _feedbackRepo = Substitute.For<IFeedbackRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _notification = Substitute.For<INotificationService>();
        _userRepo = Substitute.For<IUserRepository>();

        var user = User.Create("patient1", "patient1@test.com", "hash", UserRole.Patient, fullName: "Nguyễn Văn A");
        _patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1));
        _patient.User = user;

        _patientRepo.GetByUserIdAsync(_patient.UserId, Arg.Any<CancellationToken>()).Returns(_patient);
    }

    /// <summary>
    /// Tạo feedback hợp lệ (rating 1-5) phải gọi AddAsync 1 lần và trả về DTO — tên khách hàng trong DTO
    /// đã bị che 1 phần (NameMasker.MaskName) vì lý do riêng tư.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRating_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        var result = await handler.Handle(new CreateFeedbackCommand(_patient.UserId, 5, "Rất tốt!"), CancellationToken.None);

        await _feedbackRepo.Received(1).AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
        result.CustomerName.Should().Be(NameMasker.MaskName("Nguyễn Văn A"));
        result.Rating.Should().Be(5);
    }

    /// <summary>
    /// Feedback mới tạo phải có trạng thái Pending mặc định.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewFeedback_StatusIsPending()
    {
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        var result = await handler.Handle(new CreateFeedbackCommand(_patient.UserId, 4, "Ổn"), CancellationToken.None);

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Feedback mới phải sinh thông báo cho toàn bộ Owner — /owner/feedback là nơi họ theo dõi đánh giá.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRating_NotifiesOwners()
    {
        var ownerId = Guid.NewGuid();
        _userRepo.GetUserIdsByRoleAsync("Owner", Arg.Any<CancellationToken>()).Returns(new[] { ownerId });
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        await handler.Handle(new CreateFeedbackCommand(_patient.UserId, 5, "Rất tốt!"), CancellationToken.None);

        await _notification.Received(1).CreateForMultipleUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(ownerId)),
            Arg.Is<CreateNotificationRequest>(r =>
                r.Type == NotificationType.Service && r.RelatedEntityType == "Feedback"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating không hợp lệ bị chặn từ đầu thì không được gửi thông báo rác cho Owner.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidRating_DoesNotNotifyOwners()
    {
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        Func<Task> act = () => handler.Handle(new CreateFeedbackCommand(_patient.UserId, 9, "Sai"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _notification.DidNotReceive().CreateForMultipleUsersAsync(
            Arg.Any<IEnumerable<Guid>>(), Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating dưới 1 phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingBelowOne_ThrowsValidationException()
    {
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        Func<Task> act = () => handler.Handle(new CreateFeedbackCommand(_patient.UserId, 0, "Quá tệ"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _feedbackRepo.DidNotReceive().AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating trên 5 phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingAboveFive_ThrowsValidationException()
    {
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        Func<Task> act = () => handler.Handle(new CreateFeedbackCommand(_patient.UserId, 6, "Xuất sắc"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _feedbackRepo.DidNotReceive().AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating ở biên dưới (=1) là hợp lệ, không được ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingBoundaryOne_IsValid()
    {
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        var result = await handler.Handle(new CreateFeedbackCommand(_patient.UserId, 1, "Tệ"), CancellationToken.None);

        result.Rating.Should().Be(1);
        await _feedbackRepo.Received(1).AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating ở biên trên (=5) là hợp lệ, không được ném ValidationException.
    /// </summary>
    [Test]
    public async Task HandleAsync_RatingBoundaryFive_IsValid()
    {
        var handler = new CreateFeedbackHandler(_feedbackRepo, _patientRepo, _notification, _userRepo);

        var result = await handler.Handle(new CreateFeedbackCommand(_patient.UserId, 5, "Xuất sắc"), CancellationToken.None);

        result.Rating.Should().Be(5);
        await _feedbackRepo.Received(1).AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }
}
