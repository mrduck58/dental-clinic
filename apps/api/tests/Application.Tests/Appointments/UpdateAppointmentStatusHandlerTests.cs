using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Appointments;

[TestFixture]
public class UpdateAppointmentStatusHandlerTests
{
    private IAppointmentRepository _repo = null!;
    private UpdateAppointmentStatusHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IAppointmentRepository>();
        _handler = new UpdateAppointmentStatusHandler(_repo);
    }

    private static Appointment MakeAppointment() =>
        Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1));

    // ── ConfirmAsync ──────────────────────────────────────────────────────────

    /// <summary>
    /// Xác nhận lịch hẹn tồn tại phải gọi UpdateAsync đúng 1 lần để lưu trạng thái mới,
    /// gọi 0 lần sẽ không persist được thay đổi, gọi nhiều lần sẽ sinh race condition.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_ExistingAppointment_CallsUpdateAsyncOnce()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.ConfirmAsync(id);

        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi xác nhận, trạng thái lịch hẹn phải chuyển sang Confirmed,
    /// để bệnh nhân nhận được thông báo đúng trạng thái trên app.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_ExistingAppointment_SetsStatusToConfirmed()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.ConfirmAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    /// <summary>
    /// Lịch hẹn mới tạo luôn ở trạng thái Pending, ConfirmAsync không được bỏ qua bước này
    /// và phải thực sự thay đổi status thay vì chỉ gọi Update mà không mutate entity.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_ExistingAppointment_StatusWasPendingBeforeConfirm()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        appt.Status.Should().Be(AppointmentStatus.Pending); // trạng thái ban đầu

        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);
        await _handler.ConfirmAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Confirmed);
    }

    /// <summary>
    /// appointmentId không tồn tại phải ném KeyNotFoundException với message chứa id,
    /// để controller tra về 404 và log đúng id bị thiếu để debug.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.ConfirmAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{id}*");
    }

    /// <summary>
    /// Khi lịch hẹn không tìm thấy, UpdateAsync không được gọi để tránh lưu entity rỗng vào DB.
    /// </summary>
    [Test]
    public async Task ConfirmAsync_NonExistentAppointment_DoesNotCallUpdateAsync()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Assert.CatchAsync(() => _handler.ConfirmAsync(id));

        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    // ── CancelAsync ───────────────────────────────────────────────────────────

    /// <summary>
    /// Hủy lịch hẹn tồn tại phải gọi UpdateAsync đúng 1 lần để persist trạng thái Cancelled.
    /// </summary>
    [Test]
    public async Task CancelAsync_ExistingAppointment_CallsUpdateAsyncOnce()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.CancelAsync(id);

        await _repo.Received(1).UpdateAsync(appt, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Sau khi hủy, trạng thái lịch hẹn phải chuyển sang Cancelled,
    /// để bệnh nhân không thể vào khám theo lịch đã bị hủy.
    /// </summary>
    [Test]
    public async Task CancelAsync_ExistingAppointment_SetsStatusToCancelled()
    {
        var id = Guid.NewGuid();
        var appt = MakeAppointment();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(appt);

        await _handler.CancelAsync(id);

        appt.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    /// <summary>
    /// appointmentId không tồn tại khi hủy phải ném KeyNotFoundException với message chứa id.
    /// </summary>
    [Test]
    public async Task CancelAsync_NonExistentAppointment_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Func<Task> act = () => _handler.CancelAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{id}*");
    }

    /// <summary>
    /// Khi lịch hẹn không tìm thấy để hủy, UpdateAsync không được gọi.
    /// </summary>
    [Test]
    public async Task CancelAsync_NonExistentAppointment_DoesNotCallUpdateAsync()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Appointment?)null);

        Assert.CatchAsync(() => _handler.CancelAsync(id));

        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}
