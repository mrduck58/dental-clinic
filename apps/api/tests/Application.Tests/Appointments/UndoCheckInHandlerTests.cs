using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Appointments;

/// <summary>
/// Hoàn tác một lần check-in bấm nhầm. Điểm mấu chốt: lịch quay về đâu phụ thuộc NGUỒN của nó —
/// lịch đặt từ xa còn trạng thái cũ để quay về, lịch lập tại quầy thì không.
/// </summary>
[TestFixture]
public class UndoCheckInHandlerTests
{
    private IAppointmentRepository _repo = null!;
    private IPatientRepository _patientRepo = null!;
    private ICurrentUserService _currentUser = null!;
    private UndoCheckInAppointmentHandler _handler = null!;

    private static readonly Guid StaffUserId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IAppointmentRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _currentUser = Substitute.For<ICurrentUserService>();

        _repo.GetDentistUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Staff");
        _currentUser.UserId.Returns(StaffUserId);

        _handler = new UndoCheckInAppointmentHandler(
            _repo, _patientRepo,
            Substitute.For<IActivityLogService>(),
            Substitute.For<INotificationService>(),
            _currentUser);
    }

    private Appointment SeedOnlineCheckedIn()
    {
        var appointment = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));
        appointment.Confirm();
        appointment.CheckIn();
        _repo.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        return appointment;
    }

    private Appointment SeedWalkInCheckedIn()
    {
        var appointment = Appointment.CreateWalkIn(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(30));
        _repo.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        return appointment;
    }

    private Task Undo(Appointment appointment) =>
        _handler.Handle(new UndoCheckInCommand(appointment.Id), CancellationToken.None);

    /// <summary>Lịch bệnh nhân tự đặt quay về hàng chờ xác nhận — đúng chỗ nó đến trước khi bị bấm nhầm.</summary>
    [Test]
    public async Task Undo_OnlineAppointment_GoesBackToPending()
    {
        var appointment = SeedOnlineCheckedIn();

        await Undo(appointment);

        appointment.Status.Should().Be(AppointmentStatus.Pending);
        appointment.CheckedInAt.Should().BeNull();
        appointment.CancelledAt.Should().BeNull("hoàn tác không phải là hủy lịch");
        await _repo.Received(1).UpdateAsync(appointment, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lịch tại quầy sinh ra ngay tại lúc check-in nên không có trạng thái nào trước đó để quay về:
    /// hoàn tác chỉ có thể là hủy hẳn, và phải ghi rõ ai hủy để báo cáo không tính nhầm thành
    /// bệnh nhân bỏ hẹn.
    /// </summary>
    [Test]
    public async Task Undo_WalkInAppointment_IsCancelledWithReason()
    {
        var appointment = SeedWalkInCheckedIn();

        await Undo(appointment);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancellationReason.Should().Be(CancellationReason.Other);
        appointment.CancellationNote.Should().Be(Appointment.UndoCheckInCancellationNote);
        appointment.CancelledByUserId.Should().Be(StaffUserId);
        appointment.CheckedInAt.Should().BeNull();
    }

    /// <summary>Kết quả trả về nói rõ đã xảy ra chuyện gì, vì hai nguồn cho hai kết cục khác nhau.</summary>
    [Test]
    public async Task Undo_ReportsOriginAndResultingStatus()
    {
        var appointment = SeedWalkInCheckedIn();

        var result = await _handler.Handle(new UndoCheckInCommand(appointment.Id), CancellationToken.None);

        result.Origin.Should().Be(nameof(AppointmentOrigin.WalkIn));
        result.Status.Should().Be(nameof(AppointmentStatus.Cancelled));
    }

    /// <summary>
    /// Bệnh nhân đang chờ tới lượt được xếp theo QueueOrder; bỏ sót hai cột này thì người vừa bị
    /// gỡ check-in vẫn nằm trong hàng đợi của bác sĩ dù không còn ở trạng thái CheckedIn.
    /// </summary>
    [Test]
    public async Task Undo_ClearsQueuePosition()
    {
        var appointment = SeedOnlineCheckedIn();
        appointment.SetQueueOrder(DateTimeOffset.UtcNow.Ticks);
        appointment.SetQueueEntryOrder(DateTimeOffset.UtcNow.Ticks);

        await Undo(appointment);

        appointment.QueueOrder.Should().BeNull();
        appointment.QueueEntryOrder.Should().BeNull();
    }

    /// <summary>
    /// Bác sĩ đã gọi vào phòng thì buổi khám có thật — lúc này lịch đã có bệnh án, chỉ định, có thể
    /// cả hóa đơn treo vào. Cho gỡ tiếp là tạo ra dữ liệu mồ côi chứ không phải sửa một cú bấm nhầm.
    /// </summary>
    [TestCase(AppointmentStatus.Confirmed)]
    [TestCase(AppointmentStatus.InProgress)]
    [TestCase(AppointmentStatus.PendingPayment)]
    [TestCase(AppointmentStatus.Completed)]
    [TestCase(AppointmentStatus.Cancelled)]
    [TestCase(AppointmentStatus.NoShow)]
    public async Task Undo_NonCheckedInStatus_ThrowsConflict(AppointmentStatus status)
    {
        var appointment = SeedOnlineCheckedIn();
        typeof(Appointment).GetProperty("Status")!.SetValue(appointment, status);

        Func<Task> act = () => Undo(appointment);

        await act.Should().ThrowAsync<ConflictException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Undo_UnknownAppointment_ThrowsNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.Handle(new UndoCheckInCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Lịch đặt tại quầy phải tự đánh dấu nguồn của mình, nếu không việc hoàn tác đoán sai.</summary>
    [Test]
    public void CreateWalkIn_MarksOriginAndChecksInImmediately()
    {
        var appointment = Appointment.CreateWalkIn(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(30));

        appointment.Origin.Should().Be(AppointmentOrigin.WalkIn);
        appointment.Status.Should().Be(AppointmentStatus.CheckedIn);
        appointment.CheckedInAt.Should().NotBeNull();
    }

    /// <summary>Check-in tái khám cũng lập tại quầy — cùng một kết cục khi hoàn tác.</summary>
    [Test]
    public void CheckInFollowUp_IsAlsoWalkIn()
    {
        var appointment = Appointment.CheckInFollowUp(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        appointment.Origin.Should().Be(AppointmentOrigin.WalkIn);
    }

    /// <summary>Lịch bệnh nhân tự đặt mặc định là Online — không lệ thuộc vào việc handler nhớ set.</summary>
    [Test]
    public void Create_DefaultsToOnlineOrigin()
    {
        var appointment = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

        appointment.Origin.Should().Be(AppointmentOrigin.Online);
    }
}
