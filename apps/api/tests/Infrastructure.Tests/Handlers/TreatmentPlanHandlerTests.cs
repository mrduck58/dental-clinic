using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using static DentalClinic.API.Application.UseCases.ClinicalRecords.ClinicalRecordMappers;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class TreatmentPlanHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private INotificationService _notificationService = null!;
    // God-handler TreatmentPlanHandler (8 method) đã tách thành 8 handler MediatR.
    private CreateTreatmentPlanHandler _create = null!;
    private UpdateTreatmentPlanHandler _update = null!;
    private DeleteTreatmentPlanHandler _delete = null!;
    private GetPatientTreatmentPlansHandler _getByPatient = null!;
    private AddStepProgressHandler _addStep = null!;
    private UpdateStepProgressHandler _updateStep = null!;
    private ReorderStepProgressHandler _reorderStep = null!;
    private DeleteStepProgressHandler _deleteStep = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _patientRepo = Substitute.For<IPatientRepository>();
        _notificationService = Substitute.For<INotificationService>();

        var appointmentRepository = new AppointmentRepository(_db);
        var serviceRepository = new ServiceRepository(_db);
        var treatmentPlanRepository = new TreatmentPlanRepository(_db);
        var queryHelper = new TreatmentPlanQueryHelper(treatmentPlanRepository, appointmentRepository);

        _create = new CreateTreatmentPlanHandler(
            appointmentRepository, serviceRepository, treatmentPlanRepository, queryHelper, _patientRepo, _notificationService);
        _update = new UpdateTreatmentPlanHandler(treatmentPlanRepository, queryHelper);
        _delete = new DeleteTreatmentPlanHandler(treatmentPlanRepository, queryHelper);
        _getByPatient = new GetPatientTreatmentPlansHandler(treatmentPlanRepository, queryHelper);
        _addStep = new AddStepProgressHandler(treatmentPlanRepository, queryHelper);
        _updateStep = new UpdateStepProgressHandler(treatmentPlanRepository, queryHelper);
        _reorderStep = new ReorderStepProgressHandler(treatmentPlanRepository, queryHelper);
        _deleteStep = new DeleteStepProgressHandler(treatmentPlanRepository, queryHelper);
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

        var dto = await _create.Handle(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null), CancellationToken.None);

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

        var dto = await _create.Handle(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null), CancellationToken.None);

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
        Func<Task> act = () => _create.Handle(
            new CreateTreatmentPlanRequest(Guid.NewGuid(), Guid.NewGuid(), null, 1, null, null, null), CancellationToken.None);

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

        Func<Task> act = () => _create.Handle(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>serviceId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task CreateAsync_ServiceNotFound_ThrowsNotFoundException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p6", "d6");
        var (appointment, _) = await SeedInProgressAppointmentAsync(patient, dentist);

        Func<Task> act = () => _create.Handle(
            new CreateTreatmentPlanRequest(appointment.Id, Guid.NewGuid(), null, 1, null, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không truyền UnitPrice (null) phải mặc định lấy theo giá niêm yết của dịch vụ.</summary>
    [Test]
    public async Task CreateAsync_NullUnitPrice_DefaultsToServicePrice()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p7", "d7");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);

        var dto = await _create.Handle(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, null, 1, null, null, null), CancellationToken.None);

        dto.UnitPrice.Should().Be(service.Price);
    }

    /// <summary>Có truyền UnitPrice tùy chỉnh phải ưu tiên dùng giá đó thay vì giá niêm yết.</summary>
    [Test]
    public async Task CreateAsync_CustomUnitPrice_OverridesServicePrice()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p8", "d8");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);

        var dto = await _create.Handle(
            new CreateTreatmentPlanRequest(appointment.Id, service.Id, 999_000m, 1, null, null, null), CancellationToken.None);

        dto.UnitPrice.Should().Be(999_000m);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task UpdateAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _update.Handle(
            new UpdateTreatmentPlanRequest(Guid.NewGuid(), 100_000m, 1, null, null, null, null), CancellationToken.None);

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

        Func<Task> act = () => _update.Handle(
            new UpdateTreatmentPlanRequest(plan.Id, 300_000m, 1, null, null, null, null), CancellationToken.None);

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

        var dto = await _update.Handle(
            new UpdateTreatmentPlanRequest(plan.Id, 700_000m, 2, "11, 12", "Ghi chú mới", null, null), CancellationToken.None);

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

        Func<Task> act = () => _update.Handle(
            new UpdateTreatmentPlanRequest(plan.Id, 500_000m, 1, null, null, null, "TrangThaiSai"), CancellationToken.None);

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

        var dto = await _update.Handle(
            new UpdateTreatmentPlanRequest(plan.Id, 500_000m, 1, null, null, null, "Completed"), CancellationToken.None);

        dto.Status.Should().Be("Completed");
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task DeleteAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _delete.Handle(new DeleteTreatmentPlanCommand(Guid.NewGuid()), CancellationToken.None);

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

        Func<Task> act = () => _delete.Handle(new DeleteTreatmentPlanCommand(plan.Id), CancellationToken.None);

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

        await _delete.Handle(new DeleteTreatmentPlanCommand(plan.Id), CancellationToken.None);

        (await _db.TreatmentPlans.FindAsync(plan.Id)).Should().BeNull();
    }

    // ── GetByPatientAsync ──────────────────────────────────────────────────────

    /// <summary>Bệnh nhân chưa có liệu trình nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task GetByPatientAsync_NoPlans_ReturnsEmptyList()
    {
        var result = await _getByPatient.Handle(new GetPatientTreatmentPlansQuery(Guid.NewGuid()), CancellationToken.None);

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

        var result = await _getByPatient.Handle(new GetPatientTreatmentPlansQuery(patient.Id), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.AmountPaid.Should().Be(300_000m);
    }

    // ── AddStepProgressAsync ───────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task AddStepProgressAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _addStep.Handle(new AddStepProgressCommand(
            Guid.NewGuid(), new AddStepProgressRequest(1, "Lấy tủy", 50, null, null)), CancellationToken.None);

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

        Func<Task> act = () => _addStep.Handle(new AddStepProgressCommand(
            plan.Id, new AddStepProgressRequest(1, "   ", 50, null, null)), CancellationToken.None);

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

        Func<Task> act = () => _addStep.Handle(new AddStepProgressCommand(
            plan.Id, new AddStepProgressRequest(1, "Lấy tủy", 50, null, null)), CancellationToken.None);

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

        var dto = await _addStep.Handle(new AddStepProgressCommand(
            plan.Id, new AddStepProgressRequest(1, "Lấy tủy", 150, null, "Bước 1")), CancellationToken.None);

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
        await _addStep.Handle(new AddStepProgressCommand(plan.Id, new AddStepProgressRequest(1, "Lấy tủy", 50, null, null)), CancellationToken.None);

        var dto = await _addStep.Handle(new AddStepProgressCommand(
            plan.Id, new AddStepProgressRequest(2, "Trám bít", 100, null, null)), CancellationToken.None);

        dto.Status.Should().Be("InProgress");
        dto.StepProgress.Should().HaveCount(2);
    }

    /// <summary>Tạo sẵn liệu trình với các mục nhật ký điều trị (StepProgress) đã ghi nhận, để test
    /// trực tiếp Update/Reorder/Delete mà không cần đi qua AddStepProgressHandler.</summary>
    private async Task<TreatmentPlan> SeedPlanWithStepsAsync(
        Patient patient, Dentist dentist, Appointment appointment, Service service, params string[] stepNames)
    {
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1);
        if (stepNames.Length > 0)
        {
            var entries = stepNames.Select((name, i) => new StepProgressEntryDto
            {
                StepNumber = i + 1,
                StepName = name,
                Percent = 50,
                Date = DateOnly.FromDateTime(DateTime.Today),
                DentistName = "BS. Test",
                Note = null
            }).ToList();
            plan.UpdateStepProgress(SerializeStepProgress(entries));
        }
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }

    // ── UpdateStepProgressAsync ────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task UpdateStepProgressAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _updateStep.Handle(new UpdateStepProgressCommand(
            Guid.NewGuid(), new UpdateStepProgressRequest(0, 80, null, null)), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không có buổi khám nào đang hoạt động cho bệnh nhân của liệu trình này phải bị từ chối.</summary>
    [Test]
    public async Task UpdateStepProgressAsync_NoActiveVisit_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p20", "d20");
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow); // Pending, chưa khám
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy");

        Func<Task> act = () => _updateStep.Handle(new UpdateStepProgressCommand(
            plan.Id, new UpdateStepProgressRequest(0, 80, null, null)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>EntryIndex ngoài phạm vi danh sách nhật ký điều trị phải ném ValidationException.</summary>
    [Test]
    public async Task UpdateStepProgressAsync_OutOfRangeIndex_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p21", "d21");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy");

        Func<Task> act = () => _updateStep.Handle(new UpdateStepProgressCommand(
            plan.Id, new UpdateStepProgressRequest(5, 80, null, null)), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Sửa hợp lệ một mục theo EntryIndex phải cập nhật đúng phần trăm/ghi chú/tên bước, các
    /// mục khác giữ nguyên.</summary>
    [Test]
    public async Task UpdateStepProgressAsync_ValidRequest_UpdatesEntryFields()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p22", "d22");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy", "Trám bít");

        var dto = await _updateStep.Handle(new UpdateStepProgressCommand(
            plan.Id, new UpdateStepProgressRequest(0, 90, "Đã xong đợt 1", "Lấy tủy (cập nhật)")), CancellationToken.None);

        dto.StepProgress.Should().HaveCount(2);
        dto.StepProgress[0].StepName.Should().Be("Lấy tủy (cập nhật)");
        dto.StepProgress[0].Percent.Should().Be(90);
        dto.StepProgress[0].Note.Should().Be("Đã xong đợt 1");
        dto.StepProgress[1].StepName.Should().Be("Trám bít");
    }

    // ── ReorderStepProgressAsync ───────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task ReorderStepProgressAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _reorderStep.Handle(new ReorderStepProgressCommand(
            Guid.NewGuid(), new ReorderStepProgressRequest(new List<int> { 0 })), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không có buổi khám nào đang hoạt động cho bệnh nhân của liệu trình này phải bị từ chối.</summary>
    [Test]
    public async Task ReorderStepProgressAsync_NoActiveVisit_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p23", "d23");
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow); // Pending, chưa khám
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy", "Trám bít");

        Func<Task> act = () => _reorderStep.Handle(new ReorderStepProgressCommand(
            plan.Id, new ReorderStepProgressRequest(new List<int> { 1, 0 })), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Order không phải hoán vị hợp lệ (trùng chỉ số) phải ném ValidationException.</summary>
    [Test]
    public async Task ReorderStepProgressAsync_InvalidPermutation_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p24", "d24");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy", "Trám bít");

        Func<Task> act = () => _reorderStep.Handle(new ReorderStepProgressCommand(
            plan.Id, new ReorderStepProgressRequest(new List<int> { 0, 0 })), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Hoán vị hợp lệ phải sắp xếp lại danh sách nhật ký điều trị đúng theo Order.</summary>
    [Test]
    public async Task ReorderStepProgressAsync_ValidPermutation_ReordersEntries()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p25", "d25");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Bước A", "Bước B", "Bước C");

        var dto = await _reorderStep.Handle(new ReorderStepProgressCommand(
            plan.Id, new ReorderStepProgressRequest(new List<int> { 2, 0, 1 })), CancellationToken.None);

        dto.StepProgress.Select(s => s.StepName).Should().ContainInOrder("Bước C", "Bước A", "Bước B");
    }

    // ── DeleteStepProgressAsync ────────────────────────────────────────────────

    /// <summary>treatmentPlanId không tồn tại phải ném NotFoundException.</summary>
    [Test]
    public async Task DeleteStepProgressAsync_NonExistentPlan_ThrowsNotFoundException()
    {
        Func<Task> act = () => _deleteStep.Handle(new DeleteStepProgressCommand(Guid.NewGuid(), 0), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Không có buổi khám nào đang hoạt động cho bệnh nhân của liệu trình này phải bị từ chối.</summary>
    [Test]
    public async Task DeleteStepProgressAsync_NoActiveVisit_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p26", "d26");
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow); // Pending, chưa khám
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy");

        Func<Task> act = () => _deleteStep.Handle(new DeleteStepProgressCommand(plan.Id, 0), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>EntryIndex ngoài phạm vi danh sách nhật ký điều trị phải ném ValidationException.</summary>
    [Test]
    public async Task DeleteStepProgressAsync_OutOfRangeIndex_ThrowsValidationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p27", "d27");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy");

        Func<Task> act = () => _deleteStep.Handle(new DeleteStepProgressCommand(plan.Id, 3), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Xóa một mục không phải mục cuối cùng phải giảm đúng 1 phần tử, các mục còn lại giữ nguyên.</summary>
    [Test]
    public async Task DeleteStepProgressAsync_ValidRequest_RemovesEntry()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p28", "d28");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy", "Trám bít");

        var dto = await _deleteStep.Handle(new DeleteStepProgressCommand(plan.Id, 0), CancellationToken.None);

        dto.StepProgress.Should().ContainSingle(s => s.StepName == "Trám bít");
    }

    /// <summary>Xóa mục cuối cùng còn lại phải trả về StepProgress rỗng và StepProgressJson trong DB
    /// phải được đặt về null (không giữ lại mảng rỗng "[]").</summary>
    [Test]
    public async Task DeleteStepProgressAsync_LastEntry_ClearsStepProgress()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p29", "d29");
        var (appointment, service) = await SeedInProgressAppointmentAsync(patient, dentist);
        var plan = await SeedPlanWithStepsAsync(patient, dentist, appointment, service, "Lấy tủy");

        var dto = await _deleteStep.Handle(new DeleteStepProgressCommand(plan.Id, 0), CancellationToken.None);

        dto.StepProgress.Should().BeEmpty();
        var reloaded = await _db.TreatmentPlans.FindAsync(plan.Id);
        reloaded!.StepProgressJson.Should().BeNull();
    }
}
