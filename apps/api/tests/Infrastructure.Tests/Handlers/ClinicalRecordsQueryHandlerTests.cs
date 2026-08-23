using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

// ─────────────────────────────────────────────────────────────────────────────
// GetMyExaminationHistoryHandler / GetMyTreatmentPlansHandler /
// GetPatientMedicalHistoryHandler — 3 endpoint trước đây viết THẲNG bằng truy
// vấn EF trong AppointmentsController (không có handler), mới được tách ra
// nhưng chưa có test riêng. File này bổ sung coverage cho cả 3.
// ─────────────────────────────────────────────────────────────────────────────

[TestFixture]
public class ClinicalRecordsQueryHandlerTests
{
    private AppDbContext _db = null!;
    private GetMyExaminationHistoryHandler _myHistoryHandler = null!;
    private GetPatientMedicalHistoryHandler _patientHistoryHandler = null!;
    private ISender _sender = null!;
    private GetMyTreatmentPlansHandler _myTreatmentPlansHandler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        var patientRepository = new PatientRepository(_db);
        var appointmentRepository = new AppointmentRepository(_db);
        _myHistoryHandler = new GetMyExaminationHistoryHandler(patientRepository, appointmentRepository);
        _patientHistoryHandler = new GetPatientMedicalHistoryHandler(appointmentRepository);

        _sender = Substitute.For<ISender>();
        _myTreatmentPlansHandler = new GetMyTreatmentPlansHandler(patientRepository, _sender);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, DentistProfile dentist, Service service)> SeedPatientDentistServiceAsync(
        string patientUsername, string dentistUsername)
    {
        var patientUser = User.Create(patientUsername, $"{patientUsername}@test.com", "hash", UserRole.Patient, fullName: $"BN {patientUsername}");
        var dentistUser = User.Create(dentistUsername, $"{dentistUsername}@test.com", "hash", UserRole.Dentist, fullName: $"BS {dentistUsername}");
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        patient.User = patientUser;
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return (patient, dentist, service);
    }

    private async Task<Patient> SeedFamilyMemberAsync(Patient primaryPatient, string username, string relationship)
    {
        var user = User.Create(username, $"{username}@test.com", "hash", UserRole.Patient, fullName: $"BN {username}");
        _db.Users.Add(user);
        var member = Patient.Create(user.Id, new DateOnly(2015, 1, 1), "Nữ", primaryPatientId: primaryPatient.Id, relationship: relationship);
        member.User = user;
        _db.Patients.Add(member);
        await _db.SaveChangesAsync();
        return member;
    }

    // ══════════════════════════════════════════════════════════════════════
    // GetMyExaminationHistoryHandler
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>UserId không ứng với hồ sơ Patient nào (chưa có Patient liên kết) phải trả về danh
    /// sách rỗng thay vì ném lỗi.</summary>
    [Test]
    public async Task MyHistory_UserHasNoPatientProfile_ReturnsEmptyList()
    {
        var result = await _myHistoryHandler.Handle(
            new GetMyExaminationHistoryQuery(Guid.NewGuid(), null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Buổi hẹn còn ở trạng thái Pending (chưa khám xong) không được coi là lịch sử khám.</summary>
    [Test]
    public async Task MyHistory_OnlyPendingAppointment_ReturnsEmptyList()
    {
        var (patient, dentist, _) = await SeedPatientDentistServiceAsync("mh1", "d_mh1");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow); // Pending
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _myHistoryHandler.Handle(
            new GetMyExaminationHistoryQuery(patient.UserId, null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Buổi khám đã Completed của chính bệnh nhân phải trả về đúng thông tin (mã lịch hẹn,
    /// tên bác sĩ, dịch vụ) và quan hệ "Tôi" vì là hồ sơ của chính người đăng nhập.</summary>
    [Test]
    public async Task MyHistory_OwnCompletedAppointment_ReturnsMappedDtoWithSelfRelationship()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("mh2", "d_mh2");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id, symptoms: "Đau răng");
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _myHistoryHandler.Handle(
            new GetMyExaminationHistoryQuery(patient.UserId, null), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.AppointmentId.Should().Be(appointment.Id);
        dto.DentistName.Should().Be(dentist.FullName);
        dto.ServiceName.Should().Be(service.Name);
        dto.PatientId.Should().Be(patient.Id);
        dto.PatientRelationship.Should().Be("Tôi");
    }

    /// <summary>Buổi khám PendingPayment (đã kết thúc điều trị, chờ thanh toán) cũng phải được coi là
    /// lịch sử khám giống Completed.</summary>
    [Test]
    public async Task MyHistory_PendingPaymentAppointment_IsIncluded()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("mh3", "d_mh3");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        appointment.StartTreatment();
        appointment.EndTreatment(); // -> PendingPayment
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _myHistoryHandler.Handle(
            new GetMyExaminationHistoryQuery(patient.UserId, null), CancellationToken.None);

        result.Should().ContainSingle(d => d.AppointmentId == appointment.Id);
    }

    /// <summary>Không truyền PatientId (null) phải gộp CẢ hồ sơ chính lẫn hồ sơ thành viên gia đình,
    /// và hiển thị đúng Relationship khai báo (không phải "Tôi") cho thành viên gia đình.</summary>
    [Test]
    public async Task MyHistory_NoPatientIdFilter_IncludesFamilyMembersWithTheirRelationship()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("mh4", "d_mh4");
        var child = await SeedFamilyMemberAsync(patient, "mh4_child", "Con");

        var ownAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-1), serviceId: service.Id);
        ownAppointment.Complete();
        var childAppointment = Appointment.Create(child.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        childAppointment.Complete();
        _db.Appointments.AddRange(ownAppointment, childAppointment);
        await _db.SaveChangesAsync();

        var result = await _myHistoryHandler.Handle(
            new GetMyExaminationHistoryQuery(patient.UserId, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(d => d.PatientId == patient.Id && d.PatientRelationship == "Tôi");
        result.Should().ContainSingle(d => d.PatientId == child.Id && d.PatientRelationship == "Con");
    }

    /// <summary>Truyền PatientId cụ thể (thành viên gia đình) phải CHỈ trả về lịch sử của thành viên
    /// đó, không lẫn buổi khám của chính người đăng nhập.</summary>
    [Test]
    public async Task MyHistory_WithPatientIdFilter_ReturnsOnlyThatPatientsAppointments()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("mh5", "d_mh5");
        var child = await SeedFamilyMemberAsync(patient, "mh5_child", "Con");

        var ownAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        ownAppointment.Complete();
        var childAppointment = Appointment.Create(child.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        childAppointment.Complete();
        _db.Appointments.AddRange(ownAppointment, childAppointment);
        await _db.SaveChangesAsync();

        var result = await _myHistoryHandler.Handle(
            new GetMyExaminationHistoryQuery(patient.UserId, child.Id), CancellationToken.None);

        result.Should().ContainSingle(d => d.PatientId == child.Id);
    }

    /// <summary>Nhiều buổi khám phải được sắp xếp giảm dần theo ngày hẹn (mới nhất lên đầu).</summary>
    [Test]
    public async Task MyHistory_MultipleAppointments_OrderedByDateDescending()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("mh6", "d_mh6");
        var older = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-5), serviceId: service.Id);
        older.Complete();
        var newer = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        newer.Complete();
        _db.Appointments.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _myHistoryHandler.Handle(
            new GetMyExaminationHistoryQuery(patient.UserId, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].AppointmentId.Should().Be(newer.Id);
        result[1].AppointmentId.Should().Be(older.Id);
    }

    // ══════════════════════════════════════════════════════════════════════
    // GetPatientMedicalHistoryHandler
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Bệnh nhân chưa có buổi khám nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task PatientHistory_NoAppointments_ReturnsEmptyList()
    {
        var result = await _patientHistoryHandler.Handle(
            new GetPatientMedicalHistoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Buổi hẹn Pending (chưa khám) không được tính vào lịch sử khám bệnh của bệnh nhân.</summary>
    [Test]
    public async Task PatientHistory_PendingAppointment_ExcludedFromHistory()
    {
        var (patient, dentist, _) = await SeedPatientDentistServiceAsync("ph1", "d_ph1");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _patientHistoryHandler.Handle(
            new GetPatientMedicalHistoryQuery(patient.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Buổi khám Completed đầy đủ chẩn đoán/liệu trình/đơn thuốc phải map đúng toàn bộ dữ
    /// liệu lâm sàng vào DTO trả về.</summary>
    [Test]
    public async Task PatientHistory_CompletedAppointmentWithClinicalData_ReturnsFullyMappedDto()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("ph2", "d_ph2");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id, symptoms: "Ê buốt");
        appointment.Complete();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var diagnosis = Diagnosis.Create(appointment.Id, "K02.1",
            new DiagnosisDetails("Sâu răng", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null));
        _db.Diagnoses.Add(diagnosis);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, 500_000m, 1, teeth: "16");
        _db.TreatmentPlans.Add(plan);
        var prescription = Prescription.Create(appointment.Id, "Uống sau ăn");
        _db.Prescriptions.Add(prescription);
        var examPhoto = AppointmentPhoto.Create(appointment.Id, AppointmentPhoto.SectionExam, "/uploads/xray1.jpg", "Răng 16", "BS test");
        var materialPhoto = AppointmentPhoto.Create(appointment.Id, AppointmentPhoto.SectionMaterialRequest, "/uploads/dau-rang.jpg", null, "BS test");
        _db.AppointmentPhotos.AddRange(examPhoto, materialPhoto);
        await _db.SaveChangesAsync();

        var result = await _patientHistoryHandler.Handle(
            new GetPatientMedicalHistoryQuery(patient.Id), CancellationToken.None);

        var dto = result.Should().ContainSingle().Subject;
        dto.AppointmentId.Should().Be(appointment.Id);
        dto.DentistName.Should().Be(dentist.FullName);
        dto.ServiceName.Should().Be(service.Name);
        dto.Symptoms.Should().Be("Ê buốt");
        dto.Diagnoses.Should().ContainSingle(d => d.Description == "K02.1");
        dto.TreatmentPlans.Should().ContainSingle(t => t.Description.Contains("Răng 16"));
        dto.PrescriptionItems.Should().BeEmpty(); // Prescription chưa có Item nào
        // Chỉ ảnh section "exam" — ảnh yêu cầu vật tư (material-request) không phải thứ bệnh nhân cần xem.
        dto.Photos.Should().ContainSingle(p => p.Url == "/uploads/xray1.jpg" && p.Note == "Răng 16");
    }

    /// <summary>Buổi khám PendingPayment (đã điều trị xong, chờ thu tiền) phải được tính vào lịch sử
    /// khám giống Completed.</summary>
    [Test]
    public async Task PatientHistory_PendingPaymentAppointment_IsIncluded()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("ph3", "d_ph3");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        appointment.StartTreatment();
        appointment.EndTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var result = await _patientHistoryHandler.Handle(
            new GetPatientMedicalHistoryQuery(patient.Id), CancellationToken.None);

        result.Should().ContainSingle(d => d.AppointmentId == appointment.Id);
    }

    /// <summary>Chỉ trả về buổi khám của ĐÚNG bệnh nhân được truy vấn, không lẫn buổi khám của bệnh
    /// nhân khác.</summary>
    [Test]
    public async Task PatientHistory_OnlyReturnsRequestedPatientsAppointments()
    {
        var (patientA, dentist, service) = await SeedPatientDentistServiceAsync("ph4a", "d_ph4");
        var (patientB, _, _) = await SeedPatientDentistServiceAsync("ph4b", "d_ph4b");
        var apA = Appointment.Create(patientA.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        apA.Complete();
        var apB = Appointment.Create(patientB.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        apB.Complete();
        _db.Appointments.AddRange(apA, apB);
        await _db.SaveChangesAsync();

        var result = await _patientHistoryHandler.Handle(
            new GetPatientMedicalHistoryQuery(patientA.Id), CancellationToken.None);

        result.Should().ContainSingle(d => d.AppointmentId == apA.Id);
    }

    /// <summary>Nhiều buổi khám của cùng bệnh nhân phải sắp xếp giảm dần theo ngày hẹn.</summary>
    [Test]
    public async Task PatientHistory_MultipleAppointments_OrderedByDateDescending()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("ph5", "d_ph5");
        var older = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddDays(-3), serviceId: service.Id);
        older.Complete();
        var newer = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        newer.Complete();
        _db.Appointments.AddRange(older, newer);
        await _db.SaveChangesAsync();

        var result = await _patientHistoryHandler.Handle(
            new GetPatientMedicalHistoryQuery(patient.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].AppointmentId.Should().Be(newer.Id);
        result[1].AppointmentId.Should().Be(older.Id);
    }

    // ══════════════════════════════════════════════════════════════════════
    // GetMyTreatmentPlansHandler
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>UserId không ứng với hồ sơ Patient nào phải trả về danh sách rỗng và không gọi sang
    /// GetPatientTreatmentPlansQuery.</summary>
    [Test]
    public async Task MyTreatmentPlans_UserHasNoPatientProfile_ReturnsEmptyList()
    {
        var result = await _myTreatmentPlansHandler.Handle(
            new GetMyTreatmentPlansQuery(Guid.NewGuid(), null), CancellationToken.None);

        result.Should().BeEmpty();
        await _sender.DidNotReceiveWithAnyArgs().Send(default(GetPatientTreatmentPlansQuery)!, default);
    }

    /// <summary>Truyền PatientId KHÔNG thuộc hồ sơ chính hoặc gia đình của user phải bị từ chối bằng
    /// danh sách rỗng — không cho xem liệu trình của bệnh nhân không liên quan.</summary>
    [Test]
    public async Task MyTreatmentPlans_PatientIdNotInFamily_ReturnsEmptyList()
    {
        var (patient, _, _) = await SeedPatientDentistServiceAsync("tp1", "d_tp1");
        var strangerId = Guid.NewGuid();

        var result = await _myTreatmentPlansHandler.Handle(
            new GetMyTreatmentPlansQuery(patient.UserId, strangerId), CancellationToken.None);

        result.Should().BeEmpty();
        await _sender.DidNotReceiveWithAnyArgs().Send(default(GetPatientTreatmentPlansQuery)!, default);
    }

    /// <summary>Không truyền PatientId (null) phải gộp liệu trình của CẢ hồ sơ chính lẫn từng thành
    /// viên gia đình — gọi GetPatientTreatmentPlansQuery cho từng id và nối kết quả lại.</summary>
    [Test]
    public async Task MyTreatmentPlans_NoPatientIdFilter_AggregatesOwnAndFamilyPlans()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("tp2", "d_tp2");
        var child = await SeedFamilyMemberAsync(patient, "tp2_child", "Con");

        var ownDto = new TreatmentPlanDto { Id = Guid.NewGuid(), PatientId = patient.Id, DentistId = dentist.Id, ServiceId = service.Id };
        var childDto = new TreatmentPlanDto { Id = Guid.NewGuid(), PatientId = child.Id, DentistId = dentist.Id, ServiceId = service.Id };
        _sender.Send(Arg.Is<GetPatientTreatmentPlansQuery>(q => q.PatientId == patient.Id), Arg.Any<CancellationToken>())
            .Returns(new List<TreatmentPlanDto> { ownDto });
        _sender.Send(Arg.Is<GetPatientTreatmentPlansQuery>(q => q.PatientId == child.Id), Arg.Any<CancellationToken>())
            .Returns(new List<TreatmentPlanDto> { childDto });

        var result = await _myTreatmentPlansHandler.Handle(
            new GetMyTreatmentPlansQuery(patient.UserId, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(ownDto).And.Contain(childDto);
    }

    /// <summary>Truyền PatientId là chính hồ sơ của user phải CHỈ gọi truy vấn cho patient đó, không
    /// gộp thêm liệu trình của thành viên gia đình khác.</summary>
    [Test]
    public async Task MyTreatmentPlans_PatientIdIsSelf_OnlyQueriesThatPatient()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("tp3", "d_tp3");
        await SeedFamilyMemberAsync(patient, "tp3_child", "Con");

        var ownDto = new TreatmentPlanDto { Id = Guid.NewGuid(), PatientId = patient.Id, DentistId = dentist.Id, ServiceId = service.Id };
        _sender.Send(Arg.Is<GetPatientTreatmentPlansQuery>(q => q.PatientId == patient.Id), Arg.Any<CancellationToken>())
            .Returns(new List<TreatmentPlanDto> { ownDto });

        var result = await _myTreatmentPlansHandler.Handle(
            new GetMyTreatmentPlansQuery(patient.UserId, patient.Id), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(ownDto);
        await _sender.Received(1).Send(Arg.Any<GetPatientTreatmentPlansQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Truyền PatientId là một thành viên gia đình hợp lệ phải trả về đúng liệu trình của
    /// riêng thành viên đó.</summary>
    [Test]
    public async Task MyTreatmentPlans_PatientIdIsFamilyMember_ReturnsFamilyMembersPlans()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("tp4", "d_tp4");
        var child = await SeedFamilyMemberAsync(patient, "tp4_child", "Con");

        var childDto = new TreatmentPlanDto { Id = Guid.NewGuid(), PatientId = child.Id, DentistId = dentist.Id, ServiceId = service.Id };
        _sender.Send(Arg.Is<GetPatientTreatmentPlansQuery>(q => q.PatientId == child.Id), Arg.Any<CancellationToken>())
            .Returns(new List<TreatmentPlanDto> { childDto });

        var result = await _myTreatmentPlansHandler.Handle(
            new GetMyTreatmentPlansQuery(patient.UserId, child.Id), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(childDto);
    }
}
