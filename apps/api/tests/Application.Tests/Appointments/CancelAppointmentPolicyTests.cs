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
/// Các quy tắc hủy lịch mới: chỉ hủy được khi lịch chưa diễn ra, bệnh nhân phải hủy trước 24 giờ,
/// và lý do được lưu có cấu trúc để thống kê được thay vì nối vào cột Notes dạng văn bản tự do.
/// </summary>
[TestFixture]
public class CancelAppointmentPolicyTests
{
    private IAppointmentRepository _repo = null!;
    private IPatientRepository _patientRepo = null!;
    private ICurrentUserService _currentUser = null!;
    private CancelAppointmentHandler _handler = null!;

    private static readonly Guid PatientUserId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IAppointmentRepository>();
        _patientRepo = Substitute.For<IPatientRepository>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _repo.GetDentistUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);

        _handler = new CancelAppointmentHandler(
            _repo, Substitute.For<IActivityLogService>(), Substitute.For<INotificationService>(),
            _currentUser, _patientRepo, new AppointmentChangeGuard(_currentUser, _patientRepo, _repo));
    }

    private void ActAsStaff()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Staff");
        _currentUser.UserId.Returns(Guid.NewGuid());
    }

    private void ActAsOwningPatient(Appointment appointment)
    {
        var patient = Patient.Create(PatientUserId, new DateOnly(1990, 1, 1), "Nam");
        typeof(Appointment).GetProperty("PatientId")!.SetValue(appointment, patient.Id);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserRole.Returns("Patient");
        _currentUser.UserId.Returns(PatientUserId);
        _patientRepo.GetByUserIdAsync(PatientUserId, Arg.Any<CancellationToken>()).Returns(patient);
        _patientRepo.GetFamilyMembersAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(new List<Patient>());
    }

    private Appointment SeedAppointment(int daysAhead = 5)
    {
        var appointment = Appointment.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(daysAhead));
        _repo.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        return appointment;
    }

    private Task Cancel(Appointment appointment, CancellationReason reason, string? note = null) =>
        _handler.Handle(new CancelAppointmentCommand(appointment.Id, reason, note), CancellationToken.None);

    /// <summary>Lý do phải lưu vào cột riêng để thống kê được, không nhét vào Notes như trước.</summary>
    [Test]
    public async Task Cancel_StoresReasonAsStructuredData()
    {
        ActAsStaff();
        var appointment = SeedAppointment();

        await Cancel(appointment, CancellationReason.HealthIssue);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancellationReason.Should().Be(CancellationReason.HealthIssue);
        appointment.CancelledAt.Should().NotBeNull();
        appointment.Notes.Should().BeNull("lý do không được nối vào Notes nữa");
    }

    /// <summary>Ai bấm hủy cũng phải ghi lại — bệnh nhân tự hủy khác hẳn phòng khám hủy khi làm báo cáo.</summary>
    [Test]
    public async Task Cancel_RecordsWhoCancelled()
    {
        var appointment = SeedAppointment();
        ActAsOwningPatient(appointment);

        await Cancel(appointment, CancellationReason.ChangeOfPlans);

        appointment.CancelledByUserId.Should().Be(PatientUserId);
    }

    /// <summary>Chọn "lý do khác" mà không nói khác thế nào thì không ghi nhận được gì.</summary>
    [Test]
    public async Task Cancel_OtherReasonWithoutNote_ThrowsValidation()
    {
        ActAsStaff();
        var appointment = SeedAppointment();

        Func<Task> act = () => Cancel(appointment, CancellationReason.Other, note: "   ");

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Cancel_OtherReasonWithNote_IsStoredTrimmed()
    {
        ActAsStaff();
        var appointment = SeedAppointment();

        await Cancel(appointment, CancellationReason.Other, note: "  Đi công tác đột xuất  ");

        appointment.CancellationNote.Should().Be("Đi công tác đột xuất");
    }

    /// <summary>
    /// Lỗ hổng cũ: Cancel() gán thẳng trạng thái, nên hủy được cả lịch đang khám hoặc đã hoàn thành —
    /// tạo ra lịch "đã hủy" nhưng vẫn kèm bệnh án và hóa đơn.
    /// </summary>
    [TestCase(AppointmentStatus.CheckedIn)]
    [TestCase(AppointmentStatus.InProgress)]
    [TestCase(AppointmentStatus.PendingPayment)]
    [TestCase(AppointmentStatus.Completed)]
    [TestCase(AppointmentStatus.Cancelled)]
    public async Task Cancel_NonChangeableStatus_ThrowsConflict(AppointmentStatus status)
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        typeof(Appointment).GetProperty("Status")!.SetValue(appointment, status);

        Func<Task> act = () => Cancel(appointment, CancellationReason.ChangeOfPlans);

        await act.Should().ThrowAsync<ConflictException>();
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Nhân viên hủy hộ lúc bệnh nhân gọi điện phút chót thì không bị chặn.</summary>
    [Test]
    public async Task Cancel_StaffWithinDeadline_IsAllowed()
    {
        ActAsStaff();
        var appointment = SeedAppointment();
        typeof(Appointment).GetProperty("AppointmentDate")!
            .SetValue(appointment, DateTimeOffset.UtcNow.AddHours(3));

        await Cancel(appointment, CancellationReason.DentistUnavailable);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    /// <summary>
    /// Hai vai trò thấy hai danh sách khác nhau, không phải một danh sách chung bị lọc bớt.
    /// Lễ tân không được chọn lý do cá nhân của bệnh nhân ("tôi đổi kế hoạch") — đó là đoán thay
    /// bệnh nhân và làm sai lệch báo cáo; ngược lại bệnh nhân không thấy lý do vận hành nội bộ.
    /// </summary>
    [Test]
    public void ReasonCatalog_GivesPatientsAndStaffDifferentLists()
    {
        var patientCodes = CancellationReasonCatalog.ForPatient().Select(o => o.Code).ToList();
        var staffCodes = CancellationReasonCatalog.ForStaff().Select(o => o.Code).ToList();

        patientCodes.Should().Contain(nameof(CancellationReason.ChangeOfPlans));
        patientCodes.Should().NotContain(nameof(CancellationReason.DentistUnavailable));

        staffCodes.Should().Contain(nameof(CancellationReason.DentistUnavailable));
        staffCodes.Should().NotContain(nameof(CancellationReason.ChangeOfPlans));

        // "Lý do khác" là lối thoát dùng chung cho cả hai.
        patientCodes.Should().Contain(nameof(CancellationReason.Other));
        staffCodes.Should().Contain(nameof(CancellationReason.Other));
    }

    /// <summary>
    /// Chỉ "Lý do khác" mới bắt nhập ghi chú. Các lý do còn lại đã đủ cụ thể — bắt lễ tân gõ thêm
    /// một câu cho "Bác sĩ không thể tiếp nhận" chỉ là thao tác vô nghĩa.
    /// </summary>
    [Test]
    public void ReasonCatalog_OnlyOtherRequiresANote()
    {
        var requiring = CancellationReasonCatalog.ForStaff()
            .Concat(CancellationReasonCatalog.ForPatient())
            .Where(o => o.RequiresNote)
            .Select(o => o.Code)
            .Distinct();

        requiring.Should().Equal(nameof(CancellationReason.Other));
    }

    /// <summary>
    /// Mọi giá trị enum đang dùng đều phải xuất hiện ở đúng một trong hai danh sách, nếu không sẽ có
    /// lý do không ai chọn được. Trừ ClinicUnavailable — giá trị cũ giữ lại chỉ để đọc dữ liệu đã lưu.
    /// </summary>
    [Test]
    public void ReasonCatalog_CoversEveryEnumValueExceptRetiredOnes()
    {
        var offered = CancellationReasonCatalog.ForStaff()
            .Concat(CancellationReasonCatalog.ForPatient())
            .Select(o => o.Code)
            .Distinct();

        var expected = Enum.GetNames<CancellationReason>()
            .Except([nameof(CancellationReason.ClinicUnavailable)]);

        offered.Should().BeEquivalentTo(expected);
    }

    /// <summary>
    /// Khép vòng hợp đồng với client: MỌI mã server phát ra ở endpoint danh sách lý do đều phải
    /// parse lại được khi client gửi lên. Trước đây DTO khai báo thẳng enum, mà dự án không cấu hình
    /// JsonStringEnumConverter nên System.Text.Json chỉ bind enum từ số — mọi mã chữ như
    /// "PatientRequested" bị từ chối ở bước bind và trả 400 trước khi vào controller.
    /// </summary>
    [Test]
    public void ReasonCatalog_EveryOfferedCodeParsesBack()
    {
        var codes = CancellationReasonCatalog.ForStaff()
            .Concat(CancellationReasonCatalog.ForPatient())
            .Select(o => o.Code)
            .Distinct();

        foreach (var code in codes)
        {
            var act = () => CancellationReasonCatalog.Parse(code);
            act.Should().NotThrow($"client gửi lại đúng mã '{code}' mà server vừa phát ra");
        }
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("KhongTonTai")]
    public void ReasonCatalog_RejectsUnknownCode(string? code)
    {
        var act = () => CancellationReasonCatalog.Parse(code);

        act.Should().Throw<ValidationException>();
    }

    /// <summary>Giá trị cũ vẫn phải có nhãn đọc được để nhật ký và báo cáo không hiện mã trống.</summary>
    [Test]
    public void ReasonCatalog_StillLabelsRetiredValues()
    {
        CancellationReasonCatalog.LabelOf(CancellationReason.ClinicUnavailable)
            .Should().NotBeNullOrWhiteSpace();
    }
}
