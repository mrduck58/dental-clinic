using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class TreatmentPlanHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private INotificationService _notificationService = null!;
    private TreatmentPlanHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _patientRepo = Substitute.For<IPatientRepository>();
        _notificationService = Substitute.For<INotificationService>();
        _handler = new TreatmentPlanHandler(_db, _patientRepo, _notificationService);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Appointment appointment, Service service)> SeedInProgressAppointmentAsync(
        Patient patient, Dentist dentist)
    {
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);

        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return (appointment, service);
    }

    /// <summary>Tạo kế hoạch điều trị cho bệnh nhân CÓ tài khoản phải gửi đúng 1 thông báo, nội dung
    /// nêu rõ tên dịch vụ và tham chiếu đúng TreatmentPlan vừa tạo.</summary>
    [Test]
    public async Task CreateAsync_PatientHasAccount_SendsNotificationToPatientUser()
    {
        var patientUser = User.Create("p3", "p3@test.com", "hash", "Patient");
        var dentistUser = User.Create("d3", "d3@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();
        _patientRepo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);

        var dto = await _handler.CreateAsync(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null));

        await _notificationService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r =>
                r.UserId == patientUser.Id &&
                r.Type == "service" &&
                r.Body.Contains("Trám răng") &&
                r.RelatedEntityType == "TreatmentPlan" &&
                r.RelatedEntityId == dto.Id.ToString()),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Bệnh nhân không có tài khoản liên kết không được gửi thông báo, không ném lỗi.</summary>
    [Test]
    public async Task CreateAsync_PatientHasNoAccount_DoesNotSendNotificationOrThrow()
    {
        var dentistUser = User.Create("d4", "d4@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 3);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();
        _patientRepo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);

        var dto = await _handler.CreateAsync(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null));

        dto.Should().NotBeNull();
        await _notificationService.DidNotReceive().CreateAsync(
            Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }

    private async Task<(Patient patient, Dentist dentist)> SeedPatientAndDentistAsync(string patientUsername, string dentistUsername)
    {
        var patientUser = User.Create(patientUsername, $"{patientUsername}@test.com", "hash", "Patient");
        var dentistUser = User.Create(dentistUsername, $"{dentistUsername}@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();
        _patientRepo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        return (patient, dentist);
    }

    // ── CreateAsync: validation ───────────────────────────────────────────────

    /// <summary>appointmentId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task CreateAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.CreateAsync(
            new CreateTreatmentPlanRequest(Guid.NewGuid(), Guid.NewGuid(), null, 1, null, null, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Chỉ được thêm liệu trình khi cuộc hẹn đang InProgress; lịch Pending phải bị từ chối
    /// bằng ValidationException.</summary>
    [Test]
    public async Task CreateAsync_AppointmentNotInProgress_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p5", "d5");
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow); // Pending
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.CreateAsync(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>serviceId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task CreateAsync_ServiceNotFound_ThrowsNotFoundException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p6", "d6");
        var (appointment, _) = await SeedInProgressAppointmentAsync(patient, dentist);

        Func<Task> act = () => _handler.CreateAsync(
            new CreateTreatmentPlanRequest(appointment.Id, Guid.NewGuid(), null, 1, null, null, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không truyền UnitPrice (null) phải mặc định lấy theo giá niêm yết của dịch vụ.</summary>
    [Test]
    public async Task CreateAsync_NullUnitPrice_DefaultsToServicePrice()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p7", "d7");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);

        var dto = await _handler.CreateAsync(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null));

        dto.UnitPrice.Should().Be(service.Price);
    }

    /// <summary>Có truyền UnitPrice tùy chỉnh phải ưu tiên dùng giá đó thay vì giá niêm yết.</summary>
    [Test]
    public async Task CreateAsync_CustomUnitPrice_OverridesServicePrice()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p8", "d8");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);

        var dto = await _handler.CreateAsync(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, 999_000m, 1, null, null, null));

        dto.UnitPrice.Should().Be(999_000m);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task UpdateAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.UpdateAsync(
            new UpdateTreatmentPlanRequest(Guid.NewGuid(), 100_000m, 1, null, null, null, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Tổng chi phí mới (UnitPrice * Quantity) nhỏ hơn số tiền đã thu phải bị từ chối bằng
    /// ValidationException — tránh để công nợ âm khi khách đã trả nhiều hơn giá mới.
    /// </summary>
    [Test]
    public async Task UpdateAsync_NewTotalLessThanAmountPaid_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p9", "d9");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        var invoice = Invoice.IssuePlanInstallment(
            appointment.Id, plan.Id, "INV001", "Đặt cọc", 400_000m, DentalClinic.API.Domain.Enums.PaymentMethod.Cash);
        invoice.MarkAsPaid(DentalClinic.API.Domain.Enums.PaymentMethod.Cash);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.UpdateAsync(
            new UpdateTreatmentPlanRequest(plan.Id, 300_000m, 1, null, null, null, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Cập nhật hợp lệ (tổng mới >= đã thu) phải lưu đúng các trường mới.</summary>
    [Test]
    public async Task UpdateAsync_ValidRequest_UpdatesFields()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p10", "d10");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        var dto = await _handler.UpdateAsync(
            new UpdateTreatmentPlanRequest(plan.Id, 700_000m, 2, "11, 12", "Ghi chú mới", null, null));

        dto.UnitPrice.Should().Be(700_000m);
        dto.Quantity.Should().Be(2);
        dto.Teeth.Should().Be("11, 12");
        dto.Notes.Should().Be("Ghi chú mới");
    }

    /// <summary>Truyền Status không hợp lệ (không khớp enum) phải ném ValidationException.</summary>
    [Test]
    public async Task UpdateAsync_InvalidStatusString_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p11", "d11");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.UpdateAsync(
            new UpdateTreatmentPlanRequest(plan.Id, 500_000m, 1, null, null, null, "TrangThaiSai"));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Truyền Status hợp lệ phải cập nhật đúng trạng thái liệu trình.</summary>
    [Test]
    public async Task UpdateAsync_ValidStatusString_UpdatesStatus()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p12", "d12");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        var dto = await _handler.UpdateAsync(
            new UpdateTreatmentPlanRequest(plan.Id, 500_000m, 1, null, null, null, "Completed"));

        dto.Status.Should().Be("Completed");
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task DeleteAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Liệu trình đã có hóa đơn thu tiền không được xóa — phải ném ValidationException.</summary>
    [Test]
    public async Task DeleteAsync_PlanHasInvoices_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p13", "d13");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        var invoice = Invoice.IssuePlanInstallment(
            appointment.Id, plan.Id, "INV002", "Đặt cọc", 200_000m, DentalClinic.API.Domain.Enums.PaymentMethod.Cash);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.DeleteAsync(plan.Id);

        await act.Should().ThrowAsync<ValidationException>();
        (await _db.TreatmentPlans.FindAsync(plan.Id)).Should().NotBeNull();
    }

    /// <summary>Liệu trình chưa có hóa đơn nào phải xóa được bình thường.</summary>
    [Test]
    public async Task DeleteAsync_PlanWithoutInvoices_RemovesPlan()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p14", "d14");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        await _handler.DeleteAsync(plan.Id);

        (await _db.TreatmentPlans.FindAsync(plan.Id)).Should().BeNull();
    }

    // ── GetByPatientAsync ──────────────────────────────────────────────────────

    /// <summary>Bệnh nhân chưa có liệu trình nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task GetByPatientAsync_NoPlans_ReturnsEmptyList()
    {
        var result = await _handler.GetByPatientAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    /// <summary>Số tiền đã thu (AmountPaid) phải là tổng các hóa đơn đã Paid của liệu trình đó,
    /// không tính hóa đơn Unpaid.</summary>
    [Test]
    public async Task GetByPatientAsync_WithPaidAndUnpaidInvoices_SumsOnlyPaidAmount()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p15", "d15");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 1_000_000m, 1);
        _db.TreatmentPlans.Add(plan);
        var paidInvoice = Invoice.IssuePlanInstallment(
            appointment.Id, plan.Id, "INV003", "Đặt cọc", 300_000m, DentalClinic.API.Domain.Enums.PaymentMethod.Cash);
        paidInvoice.MarkAsPaid(DentalClinic.API.Domain.Enums.PaymentMethod.Cash);
        var unpaidInvoice = Invoice.IssuePlanInstallment(
            appointment.Id, plan.Id, "INV004", "Đợt 2", 200_000m, DentalClinic.API.Domain.Enums.PaymentMethod.Cash);
        _db.Invoices.AddRange(paidInvoice, unpaidInvoice);
        await _db.SaveChangesAsync();

        var result = await _handler.GetByPatientAsync(patient.Id);

        var dto = result.Should().ContainSingle().Subject;
        dto.AmountPaid.Should().Be(300_000m);
    }

    // ── AddStepProgressAsync ───────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task AddStepProgressAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.AddStepProgressAsync(
            Guid.NewGuid(), new AddStepProgressRequest(1, "Lấy tủy", 50, null, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Tên bước điều trị rỗng phải bị từ chối bằng ValidationException.</summary>
    [Test]
    public async Task AddStepProgressAsync_EmptyStepName_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p16", "d16");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.AddStepProgressAsync(
            plan.Id, new AddStepProgressRequest(1, "   ", 50, null, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Không có buổi khám nào đang InProgress cho bệnh nhân của liệu trình này phải bị từ chối —
    /// chỉ ghi nhận tiến độ khi bác sĩ đã bấm "Bắt đầu khám".
    /// </summary>
    [Test]
    public async Task AddStepProgressAsync_NoActiveVisit_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p17", "d17");
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);
        // Cuộc hẹn đã kết thúc điều trị (không còn InProgress)
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        appointment.EndTreatment();
        _db.Appointments.Add(appointment);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.AddStepProgressAsync(
            plan.Id, new AddStepProgressRequest(1, "Lấy tủy", 50, null, null));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Ghi nhận bước điều trị đầu tiên phải chuyển liệu trình từ Planned sang InProgress, và bước
    /// phải xuất hiện trong StepProgress của DTO trả về.
    /// </summary>
    [Test]
    public async Task AddStepProgressAsync_FirstStepOnPlannedPlan_TransitionsToInProgress()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p18", "d18");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        var dto = await _handler.AddStepProgressAsync(
            plan.Id, new AddStepProgressRequest(1, "Lấy tủy", 150, null, "Bước 1"));

        dto.Status.Should().Be("InProgress");
        dto.StepProgress.Should().ContainSingle(s => s.StepName == "Lấy tủy" && s.Percent == 100);
    }

    /// <summary>Ghi nhận bước tiếp theo khi liệu trình đã InProgress không được tạo lại chuyển trạng
    /// thái (đã là InProgress rồi) và phải cộng dồn bước mới vào danh sách.</summary>
    [Test]
    public async Task AddStepProgressAsync_SecondStepOnInProgressPlan_AppendsStepAndKeepsStatus()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p19", "d19");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        await _handler.AddStepProgressAsync(plan.Id, new AddStepProgressRequest(1, "Lấy tủy", 50, null, null));

        var dto = await _handler.AddStepProgressAsync(
            plan.Id, new AddStepProgressRequest(2, "Trám bít", 100, null, null));

        dto.Status.Should().Be("InProgress");
        dto.StepProgress.Should().HaveCount(2);
    }
}
