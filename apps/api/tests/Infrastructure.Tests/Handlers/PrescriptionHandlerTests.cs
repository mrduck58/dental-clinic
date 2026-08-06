using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class PrescriptionHandlerTests
{
    private AppDbContext _db = null!;
    private IPatientRepository _patientRepo = null!;
    private INotificationService _notificationService = null!;
    // God-handler PrescriptionHandler (6 method) đã tách thành 6 handler MediatR.
    private CreatePrescriptionHandler _create = null!;
    private UpdatePrescriptionHandler _update = null!;
    private AddPrescriptionItemHandler _addItem = null!;
    private UpdatePrescriptionItemHandler _updateItem = null!;
    private DeletePrescriptionItemHandler _deleteItem = null!;
    private GetPrescriptionByAppointmentHandler _getByAppointment = null!;

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
        var prescriptionRepository = new PrescriptionRepository(_db);
        var prescriptionItemRepository = new PrescriptionItemRepository(_db);

        _create = new CreatePrescriptionHandler(
            appointmentRepository, prescriptionRepository, prescriptionItemRepository, _patientRepo, _notificationService);
        _update = new UpdatePrescriptionHandler(prescriptionRepository);
        _addItem = new AddPrescriptionItemHandler(prescriptionRepository, prescriptionItemRepository);
        _updateItem = new UpdatePrescriptionItemHandler(prescriptionItemRepository, prescriptionRepository);
        _deleteItem = new DeletePrescriptionItemHandler(prescriptionItemRepository);
        _getByAppointment = new GetPrescriptionByAppointmentHandler(prescriptionRepository);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Appointment> SeedInProgressAppointmentAsync(Patient patient, DentistProfile dentist)
    {
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return appointment;
    }

    /// <summary>Tạo đơn thuốc thành công cho bệnh nhân CÓ tài khoản (UserId khác null) phải gửi đúng
    /// 1 thông báo cho tài khoản đó — trước đây bệnh nhân chỉ biết đơn thuốc khi tự vào xem hồ sơ.</summary>
    [Test]
    public async Task CreateAsync_PatientHasAccount_SendsNotificationToPatientUser()
    {
        var patientUser = User.Create("p1", "p1@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("d1", "d1@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        _patientRepo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);

        await _create.Handle(new CreatePrescriptionRequest(appointment.Id, "Ghi chú", null), CancellationToken.None);

        await _notificationService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r =>
                r.UserId == patientUser.Id &&
                r.Type == "service" &&
                r.RelatedEntityId == appointment.Id.ToString()),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Bệnh nhân KHÔNG có tài khoản liên kết (UserId null, vd. hồ sơ tạo tại quầy) không được
    /// gửi thông báo — và tuyệt đối không được ném lỗi làm hỏng luồng tạo đơn thuốc.</summary>
    [Test]
    public async Task CreateAsync_PatientHasNoAccount_DoesNotSendNotificationOrThrow()
    {
        var dentistUser = User.Create("d2", "d2@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 3);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        _patientRepo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);

        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);

        var result = await _create.Handle(new CreatePrescriptionRequest(appointment.Id, null, null), CancellationToken.None);

        result.Should().NotBeNull();
        await _notificationService.DidNotReceive().CreateAsync(
            Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }

    private async Task<(Patient patient, DentistProfile dentist)> SeedPatientAndDentistAsync(string patientUsername, string dentistUsername)
    {
        var patientUser = User.Create(patientUsername, $"{patientUsername}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create(dentistUsername, $"{dentistUsername}@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        await _db.SaveChangesAsync();
        _patientRepo.GetByIdAsync(patient.Id, Arg.Any<CancellationToken>()).Returns(patient);
        return (patient, dentist);
    }

    // ── CreateAsync: validation ───────────────────────────────────────────────

    /// <summary>appointmentId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task CreateAsync_AppointmentNotFound_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _create.Handle(new CreatePrescriptionRequest(Guid.NewGuid(), null, null), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Chỉ được tạo đơn thuốc khi cuộc hẹn đang InProgress (đang khám); lịch hẹn Pending phải bị
    /// từ chối bằng InvalidOperationException.
    /// </summary>
    [Test]
    public async Task CreateAsync_AppointmentNotInProgress_ThrowsInvalidOperationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p5", "d5");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow); // Pending
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _create.Handle(new CreatePrescriptionRequest(appointment.Id, null, null), CancellationToken.None);

        // CreatePrescriptionHandler ném DentalClinic ValidationException riêng (không phải InvalidOperationException
        // của .NET) khi buổi hẹn chưa đúng trạng thái — cập nhật theo đúng loại exception thật handler ném ra.
        await act.Should().ThrowAsync<DentalClinic.API.Domain.Exceptions.ValidationException>();
    }

    /// <summary>Mỗi cuộc hẹn chỉ được có một đơn thuốc — tạo lần 2 phải ném InvalidOperationException.</summary>
    [Test]
    public async Task CreateAsync_PrescriptionAlreadyExists_ThrowsInvalidOperationException()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p6", "d6");
        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);
        await _create.Handle(new CreatePrescriptionRequest(appointment.Id, null, null), CancellationToken.None);

        Func<Task> act = () => _create.Handle(new CreatePrescriptionRequest(appointment.Id, "Đơn khác", null), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>Danh sách thuốc truyền vào phải được tạo đầy đủ và ánh xạ đúng vào DTO trả về.</summary>
    [Test]
    public async Task CreateAsync_WithItems_CreatesAllItemsAndReturnsThemInDto()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p7", "d7");
        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);
        var items = new List<PrescriptionItemRequest>
        {
            new("Paracetamol", "500mg", 20, "viên", "Uống sau ăn", null),
            new("Amoxicillin", "500mg", 14, "viên", "Uống trước ăn", "Kiểm tra dị ứng")
        };

        var dto = await _create.Handle(new CreatePrescriptionRequest(appointment.Id, "Ghi chú đơn", items), CancellationToken.None);

        dto.Items.Should().HaveCount(2);
        dto.Items.Should().Contain(i => i.MedicineName == "Paracetamol" && i.Quantity == 20);
        dto.Items.Should().Contain(i => i.MedicineName == "Amoxicillin" && i.Notes == "Kiểm tra dị ứng");
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────────

    /// <summary>prescriptionId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task UpdateAsync_NonExistentPrescription_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _update.Handle(new UpdatePrescriptionRequest(Guid.NewGuid(), "Ghi chú"), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Cập nhật đơn thuốc tồn tại phải lưu ghi chú mới.</summary>
    [Test]
    public async Task UpdateAsync_ExistingPrescription_UpdatesNotes()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p8", "d8");
        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);
        var prescription = Prescription.Create(appointment.Id, "Ghi chú cũ");
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        var dto = await _update.Handle(new UpdatePrescriptionRequest(prescription.Id, "Ghi chú mới"), CancellationToken.None);

        dto.Notes.Should().Be("Ghi chú mới");
    }

    // ── AddItemAsync ───────────────────────────────────────────────────────────

    /// <summary>prescriptionId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task AddItemAsync_NonExistentPrescription_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _addItem.Handle(
            new AddPrescriptionItemRequest(Guid.NewGuid(), "Panadol", "500mg", 10, "viên", "Uống sau ăn", null), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Thêm thuốc vào đơn tồn tại phải lưu thành công và trả về DTO có đủ thuốc mới.</summary>
    [Test]
    public async Task AddItemAsync_ExistingPrescription_AddsItemToDto()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p9", "d9");
        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);
        var prescription = Prescription.Create(appointment.Id);
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        var dto = await _addItem.Handle(
            new AddPrescriptionItemRequest(prescription.Id, "Panadol", "500mg", 10, "viên", "Uống sau ăn", null), CancellationToken.None);

        dto.Items.Should().ContainSingle(i => i.MedicineName == "Panadol");
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────────────

    /// <summary>itemId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task UpdateItemAsync_NonExistentItem_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _updateItem.Handle(
            new UpdatePrescriptionItemRequest(Guid.NewGuid(), "Panadol", "500mg", 10, "viên", "Uống sau ăn", null), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Cập nhật thuốc tồn tại trong đơn phải lưu đúng các trường mới.</summary>
    [Test]
    public async Task UpdateItemAsync_ExistingItem_UpdatesFields()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p10", "d10");
        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);
        var prescription = Prescription.Create(appointment.Id);
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
        var item = PrescriptionItem.Create(prescription.Id, "Panadol", "500mg", 10, "viên", "Uống sau ăn");
        _db.PrescriptionItems.Add(item);
        await _db.SaveChangesAsync();

        var dto = await _updateItem.Handle(
            new UpdatePrescriptionItemRequest(item.Id, "Efferalgan", "250mg", 20, "gói", "Uống trước ăn", "Đổi thuốc"), CancellationToken.None);

        var updated = dto.Items.Should().ContainSingle().Subject;
        updated.MedicineName.Should().Be("Efferalgan");
        updated.Quantity.Should().Be(20);
        updated.Notes.Should().Be("Đổi thuốc");
    }

    // ── DeleteItemAsync ────────────────────────────────────────────────────────

    /// <summary>itemId không tồn tại phải ném KeyNotFoundException.</summary>
    [Test]
    public async Task DeleteItemAsync_NonExistentItem_ThrowsKeyNotFoundException()
    {
        Func<Task> act = () => _deleteItem.Handle(new DeletePrescriptionItemCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>Xóa thuốc tồn tại phải loại bỏ khỏi đơn thuốc.</summary>
    [Test]
    public async Task DeleteItemAsync_ExistingItem_RemovesItemFromPrescription()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p11", "d11");
        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);
        var prescription = Prescription.Create(appointment.Id);
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
        var item = PrescriptionItem.Create(prescription.Id, "Panadol", "500mg", 10, "viên", "Uống sau ăn");
        _db.PrescriptionItems.Add(item);
        await _db.SaveChangesAsync();

        await _deleteItem.Handle(new DeletePrescriptionItemCommand(item.Id), CancellationToken.None);

        (await _db.PrescriptionItems.FindAsync(item.Id)).Should().BeNull();
    }

    // ── GetByAppointmentAsync ──────────────────────────────────────────────────

    /// <summary>Cuộc hẹn chưa có đơn thuốc nào phải trả về null thay vì ném lỗi.</summary>
    [Test]
    public async Task GetByAppointmentAsync_NoPrescription_ReturnsNull()
    {
        var result = await _getByAppointment.Handle(new GetPrescriptionByAppointmentQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    /// <summary>Cuộc hẹn đã có đơn thuốc phải trả về đúng DTO tương ứng.</summary>
    [Test]
    public async Task GetByAppointmentAsync_HasPrescription_ReturnsDto()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p12", "d12");
        var appointment = await SeedInProgressAppointmentAsync(patient, dentist);
        var prescription = Prescription.Create(appointment.Id, "Ghi chú riêng");
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();

        var dto = await _getByAppointment.Handle(new GetPrescriptionByAppointmentQuery(appointment.Id), CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.Notes.Should().Be("Ghi chú riêng");
    }
}
