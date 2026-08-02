using DentalClinic.API.Application.UseCases.Queue;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

/// <summary>
/// GET api/queue/my-position (tên route thực tế xem QueueController) — vị trí hàng đợi của CHÍNH
/// bệnh nhân/thành viên gia đình đang đăng nhập. Handler phụ thuộc trực tiếp vào
/// <see cref="GetWaitingQueueHandler"/> (đã có test riêng ở GetWaitingQueueHandlerTests) để dùng
/// đúng thuật toán đánh số/thứ tự hàng đợi phòng — nên dùng instance THẬT của handler đó, không mock,
/// và dùng repository THẬT (PatientRepository/UserRepository) trên cùng AppDbContext InMemory để
/// hành vi tự tạo hồ sơ bệnh nhân (GetOrCreatePrimaryPatientAsync) được kiểm tra đúng như production.
/// </summary>
[TestFixture]
public class GetPatientQueueHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepository = null!;
    private IUserRepository _userRepository = null!;
    private GetWaitingQueueHandler _waitingQueueHandler = null!;
    private GetPatientQueueHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _patientRepository = new PatientRepository(_db);
        _userRepository = new UserRepository(_db);
        _waitingQueueHandler = new GetWaitingQueueHandler(_db);
        _handler = new GetPatientQueueHandler(_patientRepository, _userRepository, _waitingQueueHandler, _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>Tài khoản không tồn tại (và cũng chưa có hồ sơ bệnh nhân) phải trả về HasActiveQueue=false,
    /// không được ném lỗi.</summary>
    [Test]
    public async Task HandleAsync_UserNotFoundAndNoPatientProfile_ReturnsHasActiveQueueFalse()
    {
        var result = await _handler.Handle(new GetPatientQueueQuery(Guid.NewGuid(), null), CancellationToken.None);

        result.HasActiveQueue.Should().BeFalse();
    }

    /// <summary>Tài khoản đã tồn tại nhưng chưa có hồ sơ bệnh nhân (Patient) phải được TỰ ĐỘNG tạo hồ sơ
    /// bệnh nhân chính (GetOrCreatePrimaryPatientAsync) ngay trong lần gọi này.</summary>
    [Test]
    public async Task HandleAsync_UserWithoutPatientProfile_AutoCreatesPrimaryPatientRecord()
    {
        var user = User.Create("pq1", $"pq1-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Người Dùng Mới");
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetPatientQueueQuery(user.Id, null), CancellationToken.None);

        result.HasActiveQueue.Should().BeFalse();
        (await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id)).Should().NotBeNull();
    }

    /// <summary>PatientId truyền vào không phải bản thân và cũng không phải thành viên gia đình của
    /// bệnh nhân chính phải bị từ chối — trả về HasActiveQueue=false.</summary>
    [Test]
    public async Task HandleAsync_RequestedPatientIdNotFamilyMember_ReturnsHasActiveQueueFalse()
    {
        var user = User.Create("pq2", $"pq2-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân Chính");
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = user;
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetPatientQueueQuery(user.Id, Guid.NewGuid()), CancellationToken.None);

        result.HasActiveQueue.Should().BeFalse();
    }

    /// <summary>Bệnh nhân có lịch hẹn hôm nay nhưng chưa check-in (hoặc đã khám xong) thì không có hàng
    /// đợi đang hoạt động.</summary>
    [Test]
    public async Task HandleAsync_NoCheckedInOrInProgressAppointmentToday_ReturnsHasActiveQueueFalse()
    {
        var user = User.Create("pq3", $"pq3-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân pq3");
        var dentistUser = User.Create("pq3d", $"pq3d-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS pq3");
        _db.Users.AddRange(user, dentistUser);
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = user;
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        dentist.User = dentistUser;
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);
        var completedToday = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        completedToday.Complete();
        _db.Appointments.Add(completedToday);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetPatientQueueQuery(user.Id, null), CancellationToken.None);

        result.HasActiveQueue.Should().BeFalse();
    }

    /// <summary>Bệnh nhân đang chờ (CheckedIn) với một người khác check-in trước cùng phòng phải thấy
    /// đúng PeopleAhead, EstWaitMinutes (15 phút/người) và không có CurrentServingNumber (chưa ai đang
    /// khám trong phòng).</summary>
    [Test]
    public async Task HandleAsync_CheckedInWithSomeoneAhead_ReturnsPeopleAheadAndEstWaitMinutes()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentistUser = User.Create("pq4d", $"pq4d-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS pq4");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        dentist.User = dentistUser;
        _db.Dentists.Add(dentist);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "Phòng pq4", "border-primary", false));

        var userAhead = User.Create("pq4a", $"pq4a-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân Trước");
        var userTarget = User.Create("pq4t", $"pq4t-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân Mục Tiêu");
        _db.Users.AddRange(userAhead, userTarget);
        var patientAhead = Patient.Create(userAhead.Id, new DateOnly(1990, 1, 1), "Nam");
        patientAhead.User = userAhead;
        var patientTarget = Patient.Create(userTarget.Id, new DateOnly(1991, 1, 1), "Nữ");
        patientTarget.User = userTarget;
        _db.Patients.AddRange(patientAhead, patientTarget);

        var ahead = Appointment.Create(patientAhead.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(-20));
        ahead.CheckIn();
        var target = Appointment.Create(patientTarget.Id, dentist.Id, DateTimeOffset.UtcNow.AddMinutes(-5));
        target.CheckIn();
        _db.Appointments.AddRange(ahead, target);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetPatientQueueQuery(userTarget.Id, null), CancellationToken.None);

        result.HasActiveQueue.Should().BeTrue();
        result.AppointmentId.Should().Be(target.Id);
        result.PeopleAhead.Should().Be(1);
        result.EstWaitMinutes.Should().Be(15);
        result.CurrentServingNumber.Should().BeNull();
        result.Steps.Should().HaveCount(2);
        result.Steps!.Single(s => s.IsYours).QueueNumber.Should().Be(result.QueueNumber);
    }

    /// <summary>Bệnh nhân đang được khám (InProgress) phải có CurrentServingNumber CHÍNH LÀ số thứ tự
    /// của bản thân và không còn ai xếp trước (đã tới lượt).</summary>
    [Test]
    public async Task HandleAsync_InProgressAppointment_CurrentServingNumberEqualsOwnQueueNumber()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentistUser = User.Create("pq5d", $"pq5d-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS pq5");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        dentist.User = dentistUser;
        _db.Dentists.Add(dentist);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "Phòng pq5", "border-primary", false));

        var user = User.Create("pq5", $"pq5-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân Đang Khám");
        _db.Users.Add(user);
        var patient = Patient.Create(user.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = user;
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetPatientQueueQuery(user.Id, null), CancellationToken.None);

        result.HasActiveQueue.Should().BeTrue();
        result.PeopleAhead.Should().Be(0);
        result.CurrentServingNumber.Should().Be(result.QueueNumber);
    }

    /// <summary>Truyền PatientId của một thành viên gia đình hợp lệ phải trả về hàng đợi của THÀNH VIÊN
    /// đó, không phải của bệnh nhân chính đang đăng nhập.</summary>
    [Test]
    public async Task HandleAsync_ValidFamilyMemberPatientId_ReturnsQueueForFamilyMember()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dentistUser = User.Create("pq6d", $"pq6d-{Guid.NewGuid()}@test.com", "hash", "Dentist", fullName: "BS pq6");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        dentist.User = dentistUser;
        _db.Dentists.Add(dentist);
        _db.WorkSchedules.Add(WorkSchedule.Create(
            today, "morning", "dentist", "dentist", dentist.FullName, "Phòng pq6", "border-primary", false));

        var primaryUser = User.Create("pq6p", $"pq6p-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Bệnh nhân Chính pq6");
        _db.Users.Add(primaryUser);
        var primaryPatient = Patient.Create(primaryUser.Id, new DateOnly(1980, 1, 1), "Nam");
        primaryPatient.User = primaryUser;
        _db.Patients.Add(primaryPatient);
        await _db.SaveChangesAsync();

        var familyUser = User.Create("pq6f", $"pq6f-{Guid.NewGuid()}@test.com", "hash", "Patient", fullName: "Thành Viên Gia Đình");
        _db.Users.Add(familyUser);
        var familyPatient = Patient.Create(familyUser.Id, new DateOnly(2010, 1, 1), "Nữ", primaryPatientId: primaryPatient.Id, relationship: "Con");
        familyPatient.User = familyUser;
        _db.Patients.Add(familyPatient);
        var appointment = Appointment.Create(familyPatient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetPatientQueueQuery(primaryUser.Id, familyPatient.Id), CancellationToken.None);

        result.HasActiveQueue.Should().BeTrue();
        result.AppointmentId.Should().Be(appointment.Id);
    }
}

/// <summary>
/// Đổi chỗ hai bệnh nhân ĐANG CHỜ cạnh nhau trong hàng đợi (nút đẩy lên/xuống một bậc của lễ tân).
/// </summary>
[TestFixture]
public class ReorderQueuePatientHandlerTests
{
    private AppDbContext _db = null!;
    private ReorderQueuePatientHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new ReorderQueuePatientHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private (Dentist dentist, Patient patientA, Patient patientB) SeedDentistAndTwoPatients(string prefix)
    {
        var dentistUser = User.Create($"{prefix}d", $"{prefix}d-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patientA = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        var patientB = Patient.Create(Guid.Empty, new DateOnly(1991, 1, 1), "Nữ");
        _db.Dentists.Add(dentist);
        _db.Patients.AddRange(patientA, patientB);
        return (dentist, patientA, patientB);
    }

    /// <summary>Đổi chỗ với chính bệnh nhân đó (cùng AppointmentId ở cả hai vế) phải báo lỗi
    /// ValidationException.</summary>
    [Test]
    public async Task HandleAsync_SameAppointmentId_ThrowsValidationException()
    {
        var sameId = Guid.NewGuid();
        Func<Task> act = () => _handler.Handle(new ReorderQueuePatientCommand(sameId, sameId), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Lịch hẹn đầu tiên (AppointmentId) không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_AppointmentIdNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.Handle(
            new ReorderQueuePatientCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Lịch hẹn thứ hai (SwapWithAppointmentId) không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task HandleAsync_SwapWithAppointmentIdNotFound_ThrowsNotFoundException()
    {
        var (dentist, patientA, _) = SeedDentistAndTwoPatients("rq1");
        var appointment = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.CheckIn();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.Handle(
            new ReorderQueuePatientCommand(appointment.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Chỉ đổi thứ tự được giữa hai bệnh nhân đang chờ (CheckedIn); một trong hai đang khám
    /// (InProgress) hoặc chưa check-in phải báo lỗi ConflictException.</summary>
    [Test]
    public async Task HandleAsync_OneAppointmentNotCheckedIn_ThrowsConflictException()
    {
        var (dentist, patientA, patientB) = SeedDentistAndTwoPatients("rq2");
        var waiting = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow);
        waiting.CheckIn();
        var notYetCheckedIn = Appointment.Create(patientB.Id, dentist.Id, DateTimeOffset.UtcNow);
        notYetCheckedIn.Confirm();
        _db.Appointments.AddRange(waiting, notYetCheckedIn);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.Handle(
            new ReorderQueuePatientCommand(waiting.Id, notYetCheckedIn.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Cả hai đều đang chờ và đã có QueueOrder thủ công từ trước phải HOÁN ĐỔI đúng giá trị
    /// QueueOrder cho nhau (không đụng tới CheckedInAt).</summary>
    [Test]
    public async Task HandleAsync_BothCheckedInWithExistingQueueOrder_SwapsQueueOrderValues()
    {
        var (dentist, patientA, patientB) = SeedDentistAndTwoPatients("rq3");
        var a = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow);
        a.CheckIn();
        a.SetQueueOrder(100);
        var b = Appointment.Create(patientB.Id, dentist.Id, DateTimeOffset.UtcNow);
        b.CheckIn();
        b.SetQueueOrder(200);
        var aCheckedInAt = a.CheckedInAt;
        var bCheckedInAt = b.CheckedInAt;
        _db.Appointments.AddRange(a, b);
        await _db.SaveChangesAsync();

        await _handler.Handle(new ReorderQueuePatientCommand(a.Id, b.Id), CancellationToken.None);

        var reloadedA = await _db.Appointments.SingleAsync(x => x.Id == a.Id);
        var reloadedB = await _db.Appointments.SingleAsync(x => x.Id == b.Id);
        reloadedA.QueueOrder.Should().Be(200);
        reloadedB.QueueOrder.Should().Be(100);
        reloadedA.CheckedInAt.Should().Be(aCheckedInAt);
        reloadedB.CheckedInAt.Should().Be(bCheckedInAt);
    }

    /// <summary>Chưa từng đẩy lên/xuống thủ công (QueueOrder null ở cả hai) phải lùi về mốc giờ
    /// CHECK-IN làm vị trí hiệu dụng trước khi hoán đổi.</summary>
    [Test]
    public async Task HandleAsync_BothCheckedInWithoutExplicitQueueOrder_FallsBackToCheckedInAtTicks()
    {
        var (dentist, patientA, patientB) = SeedDentistAndTwoPatients("rq4");
        var a = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow);
        a.CheckIn();
        var b = Appointment.Create(patientB.Id, dentist.Id, DateTimeOffset.UtcNow);
        b.CheckIn();
        var aTicksBeforeSwap = a.CheckedInAt!.Value.UtcTicks;
        var bTicksBeforeSwap = b.CheckedInAt!.Value.UtcTicks;
        _db.Appointments.AddRange(a, b);
        await _db.SaveChangesAsync();

        await _handler.Handle(new ReorderQueuePatientCommand(a.Id, b.Id), CancellationToken.None);

        var reloadedA = await _db.Appointments.SingleAsync(x => x.Id == a.Id);
        var reloadedB = await _db.Appointments.SingleAsync(x => x.Id == b.Id);
        reloadedA.QueueOrder.Should().Be(bTicksBeforeSwap);
        reloadedB.QueueOrder.Should().Be(aTicksBeforeSwap);
    }
}
