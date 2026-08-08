using DentalClinic.API.Application.UseCases.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.ClinicalRecords;

[TestFixture]
public class ClinicalRecordWriteGuardTests
{
    private ICurrentUserService _currentUser = null!;
    private IDentistRepository _dentistRepo = null!;
    private IAppointmentRepository _appointmentRepo = null!;
    private IDiagnosisRepository _diagnosisRepo = null!;
    private ITreatmentPlanRepository _treatmentPlanRepo = null!;
    private IPrescriptionRepository _prescriptionRepo = null!;
    private IPrescriptionItemRepository _prescriptionItemRepo = null!;

    private static readonly Guid DentistUserId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _currentUser = Substitute.For<ICurrentUserService>();
        _dentistRepo = Substitute.For<IDentistRepository>();
        _appointmentRepo = Substitute.For<IAppointmentRepository>();
        _diagnosisRepo = Substitute.For<IDiagnosisRepository>();
        _treatmentPlanRepo = Substitute.For<ITreatmentPlanRepository>();
        _prescriptionRepo = Substitute.For<IPrescriptionRepository>();
        _prescriptionItemRepo = Substitute.For<IPrescriptionItemRepository>();
    }

    private ClinicalRecordWriteGuard CreateGuard() => new(
        _currentUser, _dentistRepo, _appointmentRepo, _diagnosisRepo,
        _treatmentPlanRepo, _prescriptionRepo, _prescriptionItemRepo);

    /// <summary>Đóng vai một bác sĩ đã có hồ sơ DentistProfile; trả về id hồ sơ đó.</summary>
    private Guid ActAsDentist()
    {
        var profile = DentistProfile.Create(Guid.NewGuid(), "Nha khoa tổng quát", "N/A", 5);
        _currentUser.UserRole.Returns("Dentist");
        _currentUser.UserId.Returns(DentistUserId);
        _dentistRepo.GetByUserIdAsync(DentistUserId, Arg.Any<CancellationToken>()).Returns(profile);
        return profile.Id;
    }

    /// <summary>Guard chỉ quan tâm AppointmentId của chẩn đoán, nội dung khám để trống là đủ.</summary>
    private static DiagnosisDetails EmptyDiagnosisDetails() => new(
        GumCondition: null, OralMucosaCondition: null, GumBleeding: null, PainOnChewing: null,
        TeethCount: null, DecayedTeeth: null, WornOrBrokenTeeth: null, LooseTeeth: null,
        Tartar: null, Plaque: null, BadBreath: null,
        TmjSymptoms: null, Occlusion: null, OcclusionDeviation: null,
        MedicalHistory: null, AllergyHistory: null, Conclusion: null);

    private Appointment SeedAppointment(Guid dentistId)
    {
        var appointment = Appointment.Create(Guid.NewGuid(), dentistId, DateTimeOffset.UtcNow);
        _appointmentRepo.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        return appointment;
    }

    /// <summary>
    /// Staff/Admin/Owner không bị guard giới hạn — không được đụng tới repository nào, tránh
    /// vừa tốn truy vấn vừa chặn nhầm vai trò vận hành.
    /// </summary>
    [TestCase("Staff")]
    [TestCase("Admin")]
    [TestCase("Owner")]
    public async Task EnsureCanWriteAppointment_NonDentistRole_PassesWithoutLookup(string role)
    {
        _currentUser.UserRole.Returns(role);
        var guard = CreateGuard();

        await guard.EnsureCanWriteAppointmentAsync(Guid.NewGuid(), CancellationToken.None);

        await _appointmentRepo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Bác sĩ ghi lên ca được phân công cho mình thì đi qua bình thường.</summary>
    [Test]
    public async Task EnsureCanWriteAppointment_OwnAppointment_Passes()
    {
        var dentistId = ActAsDentist();
        var appointment = SeedAppointment(dentistId);
        var guard = CreateGuard();

        Func<Task> act = () => guard.EnsureCanWriteAppointmentAsync(appointment.Id, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    /// <summary>Bác sĩ ghi lên ca của đồng nghiệp phải bị chặn bằng ForbiddenException (403).</summary>
    [Test]
    public async Task EnsureCanWriteAppointment_OtherDentistsAppointment_ThrowsForbidden()
    {
        ActAsDentist();
        var appointment = SeedAppointment(dentistId: Guid.NewGuid());
        var guard = CreateGuard();

        Func<Task> act = () => guard.EnsureCanWriteAppointmentAsync(appointment.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    /// <summary>Tài khoản Dentist chưa có hồ sơ DentistProfile bị chặn, không được mặc định cho qua.</summary>
    [Test]
    public async Task EnsureCanWriteAppointment_DentistWithoutProfile_ThrowsForbidden()
    {
        _currentUser.UserRole.Returns("Dentist");
        _currentUser.UserId.Returns(DentistUserId);
        _dentistRepo.GetByUserIdAsync(DentistUserId, Arg.Any<CancellationToken>()).Returns((DentistProfile?)null);
        var guard = CreateGuard();

        Func<Task> act = () => guard.EnsureCanWriteAppointmentAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    /// <summary>Chẩn đoán được truy ngược về ca khám của nó để xét quyền ghi.</summary>
    [Test]
    public async Task EnsureCanWriteDiagnosis_BelongsToOtherDentist_ThrowsForbidden()
    {
        ActAsDentist();
        var appointment = SeedAppointment(dentistId: Guid.NewGuid());
        var diagnosis = Diagnosis.Create(appointment.Id, "Sâu răng", EmptyDiagnosisDetails());
        _diagnosisRepo.GetByIdAsync(diagnosis.Id, Arg.Any<CancellationToken>()).Returns(diagnosis);
        var guard = CreateGuard();

        Func<Task> act = () => guard.EnsureCanWriteDiagnosisAsync(diagnosis.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    /// <summary>
    /// Liệu trình xét thẳng TreatmentPlan.DentistId, không đi vòng qua ca khám (AppointmentId nullable).
    /// </summary>
    [Test]
    public async Task EnsureCanWriteTreatmentPlan_BelongsToOtherDentist_ThrowsForbidden()
    {
        ActAsDentist();
        var plan = TreatmentPlan.Create(
            patientId: Guid.NewGuid(), dentistId: Guid.NewGuid(), appointmentId: null,
            serviceId: Guid.NewGuid(), unitPrice: 1_000_000m, quantity: 1);
        _treatmentPlanRepo.GetByIdAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        var guard = CreateGuard();

        Func<Task> act = () => guard.EnsureCanWriteTreatmentPlanAsync(plan.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    /// <summary>Thuốc trong đơn truy ngược 2 cấp: item → đơn thuốc → ca khám.</summary>
    [Test]
    public async Task EnsureCanWritePrescriptionItem_BelongsToOtherDentist_ThrowsForbidden()
    {
        ActAsDentist();
        var appointment = SeedAppointment(dentistId: Guid.NewGuid());
        var prescription = Prescription.Create(appointment.Id, null);
        var item = PrescriptionItem.Create(
            prescription.Id, medicineName: "Amoxicillin", dosage: "500mg",
            quantity: 10, unit: "viên", usage: "Uống sau ăn");
        _prescriptionRepo.GetByIdWithItemsAsync(prescription.Id, Arg.Any<CancellationToken>()).Returns(prescription);
        _prescriptionItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var guard = CreateGuard();

        Func<Task> act = () => guard.EnsureCanWritePrescriptionItemAsync(item.Id, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
