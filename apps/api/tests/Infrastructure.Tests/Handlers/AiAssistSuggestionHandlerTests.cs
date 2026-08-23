using DentalClinic.API.Application.UseCases.AiAssist;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

/// <summary>
/// Bao phủ SuggestPrescriptionHandler — handler AI Assist chưa có test, cùng thư mục với
/// SummarizePatientHistoryHandler (đã có test tham khảo phong cách).
/// </summary>
[TestFixture]
public class AiAssistSuggestionHandlerTests
{
    private AppDbContext _db = null!;
    private IAiChatService _aiChatService = null!;
    private SuggestPrescriptionHandler _prescriptionHandler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _aiChatService = Substitute.For<IAiChatService>();
        _aiChatService.SummarizeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Gợi ý test");

        var appointmentRepository = new AppointmentRepository(_db);
        var medicineRepository = new MedicineRepository(_db);
        _prescriptionHandler = new SuggestPrescriptionHandler(_aiChatService, appointmentRepository, medicineRepository);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Patient patient, DentistProfile dentist, Service service)> SeedPatientDentistServiceAsync(
        string patientUsername, string dentistUsername)
    {
        var patientUser = User.Create(patientUsername, $"{patientUsername}@test.com", "hash", UserRole.Patient, fullName: "Bệnh nhân Test");
        var dentistUser = User.Create(dentistUsername, $"{dentistUsername}@test.com", "hash", UserRole.Dentist, fullName: "BS. Test");
        _db.Users.AddRange(patientUser, dentistUser);

        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);

        var service = Service.Create("Trám răng", 300000m, 30, "Trám răng sâu");
        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        return (patient, dentist, service);
    }

    private static DiagnosisDetails EmptyDetails(string? allergyHistory = null) => new(
        GumCondition: null,
        OralMucosaCondition: null,
        GumBleeding: null,
        PainOnChewing: null,
        TeethCount: null,
        DecayedTeeth: null,
        WornOrBrokenTeeth: null,
        LooseTeeth: null,
        Tartar: null,
        Plaque: null,
        BadBreath: null,
        TmjSymptoms: null,
        Occlusion: null,
        OcclusionDeviation: null,
        MedicalHistory: null,
        AllergyHistory: allergyHistory,
        Conclusion: null);

    // ===================== SuggestPrescriptionHandler =====================

    /// <summary>
    /// Chẩn đoán, liệu trình điều trị của buổi khám hiện tại, tiền sử dị ứng và danh mục thuốc
    /// đang hoạt động phải xuất hiện trong nội dung gửi cho AI; kết quả trả về đúng gợi ý AI và
    /// Disclaimer cố định.
    /// </summary>
    [Test]
    public async Task SuggestPrescription_ValidAppointmentWithDiagnosis_IncludesContextInPromptAndReturnsAiSuggestion()
    {
        var (patient, dentist, service) = await SeedPatientDentistServiceAsync("pp1", "pd1");

        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow, serviceId: service.Id);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _db.Diagnoses.Add(Diagnosis.Create(
            appointment.Id, "K02.1: Sâu răng tiến triển", EmptyDetails(allergyHistory: "Dị ứng Penicillin")));

        var plan = TreatmentPlan.Create(
            patient.Id, dentist.Id, appointment.Id, service.Id, 300000m, 1, teeth: "16", notes: "Trám composite");
        _db.Set<TreatmentPlan>().Add(plan);
        await _db.SaveChangesAsync();

        var medicine = Medicine.Create("Paracetamol", "Paracetamol", "NSX A", "viên", "Giảm đau hạ sốt");
        _db.Set<Medicine>().Add(medicine);
        await _db.SaveChangesAsync();

        var result = await _prescriptionHandler.Handle(new SuggestPrescriptionQuery(appointment.Id), CancellationToken.None);

        result.Suggestion.Should().Be("Gợi ý test");
        result.Disclaimer.Should().Contain("AI tạo tự động");

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(content =>
                content.Contains("Sâu răng tiến triển") &&
                content.Contains("Dị ứng Penicillin") &&
                content.Contains("Trám composite") &&
                content.Contains("Paracetamol")),
            "PrescriptionSuggestion",
            Arg.Any<CancellationToken>());
    }

    /// <summary>Không tìm thấy lịch hẹn phải ném NotFoundException, không NullReferenceException.</summary>
    [Test]
    public async Task SuggestPrescription_NonExistentAppointment_ThrowsNotFoundException()
    {
        var nonExistentId = Guid.NewGuid();

        Func<Task> act = () => _prescriptionHandler.Handle(new SuggestPrescriptionQuery(nonExistentId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _aiChatService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lịch hẹn tồn tại nhưng chưa lưu phiếu khám (chưa có Diagnosis) phải ném ValidationException,
    /// không gọi AI.
    /// </summary>
    [Test]
    public async Task SuggestPrescription_AppointmentWithoutDiagnosis_ThrowsValidationException()
    {
        var (patient, dentist, _) = await SeedPatientDentistServiceAsync("pp2", "pd2");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _prescriptionHandler.Handle(new SuggestPrescriptionQuery(appointment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _aiChatService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Không có tiền sử dị ứng ghi nhận phải nhắc bác sĩ tự hỏi lại thay vì bỏ trống.</summary>
    [Test]
    public async Task SuggestPrescription_NoAllergyHistoryRecorded_PromptRemindsDentistToAsk()
    {
        var (patient, dentist, _) = await SeedPatientDentistServiceAsync("pp3", "pd3");
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        _db.Diagnoses.Add(Diagnosis.Create(appointment.Id, "Viêm lợi nhẹ", EmptyDetails()));
        await _db.SaveChangesAsync();

        await _prescriptionHandler.Handle(new SuggestPrescriptionQuery(appointment.Id), CancellationToken.None);

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(content => content.Contains("cần hỏi lại bệnh nhân")),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

}
