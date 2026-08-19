using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Appointments;

[TestFixture]
public class SlotHoldHandlerTests
{
    private ISlotHoldRepository _slotHoldRepo = null!;
    private IAppointmentRepository _appointmentRepo = null!;
    private IPatientRepository _patientRepo = null!;
    private ICurrentUserService _currentUser = null!;
    private ISlotNotifier _slotNotifier = null!;
    private HoldSlotHandler _handler = null!;

    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _dentistId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _slotHoldRepo = Substitute.For<ISlotHoldRepository>();
        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _slotNotifier = Substitute.For<ISlotNotifier>();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_userId);
        _currentUser.UserRole.Returns("Patient");

        var primaryPatient = Patient.Create(_userId, null);
        typeof(Patient).GetProperty(nameof(Patient.Id))!.SetValue(primaryPatient, _patientId);
        _patientRepo.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(primaryPatient);

        _appointmentRepo.GetByDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new List<Appointment>());

        _handler = new HoldSlotHandler(
            _slotHoldRepo,
            _appointmentRepo,
            _patientRepo,
            _currentUser,
            _slotNotifier);
    }

    [Test]
    public async Task HoldSlot_WhenEligible_ShouldSucceedWith5MinuteExpiry()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var command = new HoldSlotCommand(_patientId, _dentistId, tomorrow, "08:00 - 09:00");

        _slotHoldRepo.GetFailedHoldCountTodayAsync(_patientId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _slotHoldRepo.GetActiveHoldForSlotAsync(_dentistId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((AppointmentSlotHold?)null);
        _slotHoldRepo.GetActiveHoldForPatientAsync(_patientId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((AppointmentSlotHold?)null);

        var result = await _handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        result.RemainingSeconds.Should().Be(300);
        await _slotHoldRepo.Received(1).AddAsync(Arg.Any<AppointmentSlotHold>(), Arg.Any<CancellationToken>());
        await _slotNotifier.Received(1).NotifySlotHeldAsync(
            _dentistId,
            tomorrow,
            "08:00 - 09:00",
            _patientId,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HoldSlot_WhenExceeding3FailedHoldsToday_ShouldThrowConflictException()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var command = new HoldSlotCommand(_patientId, _dentistId, tomorrow, "08:00 - 09:00");

        _slotHoldRepo.GetFailedHoldCountTodayAsync(_patientId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(3);

        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*3 lần giữ chỗ không thành công*");
    }

    [Test]
    public async Task HoldSlot_WhenSlotAlreadyHeldByAnotherPatient_ShouldThrowConflictException()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var command = new HoldSlotCommand(_patientId, _dentistId, tomorrow, "08:00 - 09:00");

        var otherPatientId = Guid.NewGuid();
        var existingHold = AppointmentSlotHold.Create(
            otherPatientId,
            Guid.NewGuid(),
            _dentistId,
            DateTimeOffset.UtcNow.AddHours(20),
            "08:00 - 09:00",
            DateTimeOffset.UtcNow);

        _slotHoldRepo.GetFailedHoldCountTodayAsync(_patientId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _slotHoldRepo.GetActiveHoldForSlotAsync(_dentistId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(existingHold);

        var act = () => _handler.Handle(command);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*đang được một bệnh nhân khác giữ tạm*");
    }

    [Test]
    public async Task HoldSlot_WhenPatientReRequestsSameSlot_ShouldReturnExistingHoldWithoutResettingTimer()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var command = new HoldSlotCommand(_patientId, _dentistId, tomorrow, "08:00 - 09:00");

        var apptDateTime = tomorrow.ToDateTime(new TimeOnly(8, 0));
        var apptDateUtc = new DateTimeOffset(apptDateTime, TimeSpan.FromHours(7)).ToUniversalTime();

        var existingHold = AppointmentSlotHold.Create(
            _patientId,
            _userId,
            _dentistId,
            apptDateUtc,
            "08:00 - 09:00",
            DateTimeOffset.UtcNow.AddMinutes(-2)); // Created 2 mins ago -> 3 mins left

        _slotHoldRepo.GetFailedHoldCountTodayAsync(_patientId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(0);
        _slotHoldRepo.GetActiveHoldForSlotAsync(_dentistId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(existingHold);
        _slotHoldRepo.GetActiveHoldForPatientAsync(_patientId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(existingHold);

        var result = await _handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        result.HoldId.Should().Be(existingHold.Id);
        result.RemainingSeconds.Should().BeLessThanOrEqualTo(180);
        await _slotHoldRepo.DidNotReceive().AddAsync(Arg.Any<AppointmentSlotHold>(), Arg.Any<CancellationToken>());
    }
}
