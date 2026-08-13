using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class GetClinicFeedbackEligibilityHandlerTests
{
    private IAppointmentRepository _appointmentRepo = null!;
    private IPatientRepository _patientRepo = null!;
    private IFeedbackRepository _feedbackRepo = null!;
    private GetClinicFeedbackEligibilityHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _feedbackRepo = Substitute.For<IFeedbackRepository>();
        _handler = new GetClinicFeedbackEligibilityHandler(_appointmentRepo, _patientRepo, _feedbackRepo);
    }

    private static Patient MakePatient(string fullName = "Nguyễn Văn A")
    {
        var user = User.Create($"pt-{Guid.NewGuid()}", $"{Guid.NewGuid()}@test.com", "hash", UserRole.Patient, fullName: fullName);
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = user;
        return patient;
    }

    /// <summary>Tài khoản gọi chưa có hồ sơ bệnh nhân phải trả CanReview = false.</summary>
    [Test]
    public async Task HandleAsync_NoPatientProfile_ReturnsNotEligible()
    {
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var result = await _handler.Handle(new GetClinicFeedbackEligibilityQuery(Guid.NewGuid()), CancellationToken.None);

        result.CanReview.Should().BeFalse();
        result.HasCompletedFirstVisit.Should().BeFalse();
        result.MyFeedback.Should().BeNull();
    }

    /// <summary>Chưa hoàn tất lần khám nào tại phòng khám thì chưa đủ điều kiện gửi đánh giá.</summary>
    [Test]
    public async Task HandleAsync_NoCompletedVisit_ReturnsNotEligible()
    {
        var patient = MakePatient();
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);
        _appointmentRepo.CountOverallCompletedVisitsAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(0);

        var result = await _handler.Handle(new GetClinicFeedbackEligibilityQuery(patient.UserId), CancellationToken.None);

        result.CanReview.Should().BeFalse();
        result.HasCompletedFirstVisit.Should().BeFalse();
    }

    /// <summary>Đã hoàn tất lần khám đầu tiên và chưa từng gửi đánh giá phòng khám thì đủ điều kiện.</summary>
    [Test]
    public async Task HandleAsync_CompletedVisitAndNoExistingFeedback_ReturnsEligible()
    {
        var patient = MakePatient("Trần Thị B");
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);
        _appointmentRepo.CountOverallCompletedVisitsAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(1);
        _feedbackRepo.GetByPatientIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns((Feedback?)null);

        var result = await _handler.Handle(new GetClinicFeedbackEligibilityQuery(patient.UserId), CancellationToken.None);

        result.CanReview.Should().BeTrue();
        result.HasCompletedFirstVisit.Should().BeTrue();
        result.MyFeedback.Should().BeNull();
    }

    /// <summary>Đã gửi đánh giá phòng khám trước đó (khớp theo PatientId) thì không được gửi thêm, trả kèm
    /// đánh giá cũ trong MyFeedback.</summary>
    [Test]
    public async Task HandleAsync_AlreadySubmittedFeedback_ReturnsNotEligibleWithMyFeedback()
    {
        var patient = MakePatient("Lê Văn C");
        _patientRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(patient);
        _appointmentRepo.CountOverallCompletedVisitsAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(2);
        var existingFeedback = Feedback.Create("Lê Văn C", 5, "Rất hài lòng", patient.Id);
        _feedbackRepo.GetByPatientIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(existingFeedback);

        var result = await _handler.Handle(new GetClinicFeedbackEligibilityQuery(patient.UserId), CancellationToken.None);

        result.CanReview.Should().BeFalse();
        result.HasCompletedFirstVisit.Should().BeTrue();
        result.MyFeedback.Should().NotBeNull();
        result.MyFeedback!.Rating.Should().Be(5);
    }
}
