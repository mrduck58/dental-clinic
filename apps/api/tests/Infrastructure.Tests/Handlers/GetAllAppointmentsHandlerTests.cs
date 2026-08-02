using DentalClinic.API.Application.UseCases.Booking;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class GetAllAppointmentsHandlerTests
{
    private AppDbContext _db = null!;
    private GetAllAppointmentsHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _handler = new GetAllAppointmentsHandler(_db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    // ── Seed helpers ──────────────────────────────────────────────────────────

    private async Task<(Patient patient, Dentist dentist)> SeedBasicDataAsync(
        string patientName = "Trần Thị B",
        string phone = "0901234567",
        string dentistName = "BS. Nguyễn Văn A",
        string specialization = "Nha khoa tổng quát")
    {
        var patientUser = User.Create("p1", "p1@test.com", "hash", "Patient", phone, fullName: patientName);
        var dentistUser = User.Create("d1", "d1@test.com", "hash", "Dentist", fullName: dentistName);
        _db.Users.AddRange(patientUser, dentistUser);

        var dentist = Dentist.Create(dentistUser.Id, specialization, 5);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nữ");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);

        await _db.SaveChangesAsync();
        return (patient, dentist);
    }

    // ── Không có filter ───────────────────────────────────────────────────────

    /// <summary>
    /// Không truyền filter phải trả về tất cả appointment trong DB,
    /// để staff thấy toàn bộ lịch hẹn khi cần tổng quan.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilter_ReturnsAllAppointments()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(2))
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Không có appointment nào trong DB phải trả về danh sách rỗng, không ném exception.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoAppointments_ReturnsEmptyList()
    {
        var result = await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── Ánh xạ dữ liệu ───────────────────────────────────────────────────────

    /// <summary>
    /// DTO trả về phải chứa tên bệnh nhân và bác sĩ đúng theo dữ liệu đã seed,
    /// để staff xem được đủ thông tin trên trang nhận đơn.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilter_MapsPatientAndDentistNames()
    {
        var (patient, dentist) = await SeedBasicDataAsync(
            patientName: "Nguyễn Thị C",
            dentistName: "BS. Lê Văn D",
            specialization: "Implant");
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)));
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None)).Single();

        dto.PatientName.Should().Be("Nguyễn Thị C");
        dto.DentistName.Should().Be("BS. Lê Văn D");
    }

    /// <summary>
    /// SĐT bệnh nhân phải được lấy từ bảng Users (qua navigation Patient→User),
    /// không phải từ bảng Patients vì Patient không có cột PhoneNumber.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientLinkedToUser_ReturnsPhoneFromUserRecord()
    {
        var (patient, dentist) = await SeedBasicDataAsync(phone: "0912345678");
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)));
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None)).Single();

        dto.PatientPhone.Should().Be("0912345678");
    }

    /// <summary>
    /// Appointment có triệu chứng phải ánh xạ đúng vào trường Symptoms của DTO.
    /// </summary>
    [Test]
    public async Task HandleAsync_AppointmentWithSymptoms_MapsSymptomsToDto()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1), symptoms: "Đau răng hàm trên"));
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None)).Single();

        dto.Symptoms.Should().Be("Đau răng hàm trên");
    }

    /// <summary>
    /// Status trong DTO phải là chuỗi "Pending" thay vì số enum,
    /// để frontend hiển thị badge đúng mà không cần bảng ánh xạ phụ.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewAppointment_StatusIsPendingString()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)));
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None)).Single();

        dto.Status.Should().Be("Pending");
    }

    // ── Filter theo ngày ──────────────────────────────────────────────────────

    /// <summary>
    /// Filter theo ngày chỉ trả về appointment trong khoảng [00:00, 24:00) UTC của ngày đó,
    /// để tab "Hôm nay" hiển thị đúng lịch trong ngày.
    /// </summary>
    [Test]
    public async Task HandleAsync_DateFilter_ReturnsOnlyAppointmentsOnThatDay()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var targetDate = new DateTimeOffset(2026, 6, 20, 9, 0, 0, TimeSpan.Zero);
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, targetDate),
            Appointment.Create(patient.Id, dentist.Id, targetDate.AddDays(1))
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAllAppointmentsQuery(new DateOnly(2026, 6, 20), null), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().AppointmentDate.Should().Be(targetDate);
    }

    /// <summary>
    /// Không có appointment nào trong ngày được lọc phải trả về danh sách rỗng.
    /// </summary>
    [Test]
    public async Task HandleAsync_DateFilter_NoMatchReturnsEmpty()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.Add(
            Appointment.Create(patient.Id, dentist.Id, new DateTimeOffset(2026, 6, 20, 9, 0, 0, TimeSpan.Zero))
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAllAppointmentsQuery(new DateOnly(2026, 6, 19), null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ── Filter theo status ────────────────────────────────────────────────────

    /// <summary>
    /// Filter status "Pending" chỉ trả về appointment chưa xác nhận,
    /// để tab nhận đơn online của staff không trộn lẫn đơn đã xử lý.
    /// </summary>
    [Test]
    public async Task HandleAsync_StatusPendingFilter_ReturnsOnlyPendingAppointments()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var pending = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1));
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(2));
        confirmed.Confirm();
        _db.Appointments.AddRange(pending, confirmed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAllAppointmentsQuery(null, "Pending"), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Status.Should().Be("Pending");
    }

    /// <summary>
    /// Filter status "Confirmed" chỉ trả về appointment đã xác nhận.
    /// </summary>
    [Test]
    public async Task HandleAsync_StatusConfirmedFilter_ReturnsOnlyConfirmedAppointments()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var pending = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1));
        var confirmed = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(2));
        confirmed.Confirm();
        _db.Appointments.AddRange(pending, confirmed);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAllAppointmentsQuery(null, "Confirmed"), CancellationToken.None);

        result.Should().HaveCount(1);
        result.Single().Status.Should().Be("Confirmed");
    }

    /// <summary>
    /// Status string không hợp lệ (không match enum) phải bỏ qua filter và trả về tất cả,
    /// để tránh exception khi client truyền sai giá trị.
    /// </summary>
    [Test]
    public async Task HandleAsync_InvalidStatusFilter_IgnoresFilterAndReturnsAll()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(2))
        );
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new GetAllAppointmentsQuery(null, "TrangThaiKhongTonTai"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    // ── Thứ tự trả về ─────────────────────────────────────────────────────────

    /// <summary>
    /// Kết quả phải được sắp xếp theo CreatedAt giảm dần (mới nhất lên đầu),
    /// để staff thấy ngay đơn mới nhất mà không phải scroll xuống.
    /// </summary>
    [Test]
    public async Task HandleAsync_MultipleAppointments_ReturnedOrderedByCreatedAtDescending()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.AddRange(
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)),
            Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(2))
        );
        await _db.SaveChangesAsync();

        var result = (await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None)).ToList();

        result.Should().BeInDescendingOrder(r => r.CreatedAt);
    }

    /// <summary>
    /// Lịch hẹn không gắn dịch vụ (ServiceId null) phải ánh xạ ServiceName thành null thay vì
    /// ném NullReferenceException khi truy cập a.Service.Name.
    /// </summary>
    [Test]
    public async Task HandleAsync_AppointmentWithoutService_MapsServiceNameAsNull()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        _db.Appointments.Add(Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1)));
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None)).Single();

        dto.ServiceName.Should().BeNull();
    }

    /// <summary>
    /// Lịch hẹn đã check-in phải ánh xạ đúng CheckedInAt vào DTO, để staff biết thời điểm bệnh
    /// nhân đến quầy khi xem danh sách.
    /// </summary>
    [Test]
    public async Task HandleAsync_CheckedInAppointment_MapsCheckedInAtToDto()
    {
        var (patient, dentist) = await SeedBasicDataAsync();
        var appt = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(1));
        appt.CheckIn();
        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        var dto = (await _handler.Handle(new GetAllAppointmentsQuery(null, null), CancellationToken.None)).Single();

        dto.CheckedInAt.Should().NotBeNull();
        dto.Status.Should().Be("CheckedIn");
    }
}
