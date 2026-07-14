using DentalClinic.API.Application.UseCases.Appointments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class SummarizePatientHistoryHandlerTests
{
    private AppDbContext _db = null!;
    private IAiChatService _aiChatService = null!;
    private SummarizePatientHistoryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _aiChatService = Substitute.For<IAiChatService>();
        _aiChatService.SummarizeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Tóm tắt test");

        _handler = new SummarizePatientHistoryHandler(_aiChatService, _db);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    /// <summary>
    /// Dữ liệu chẩn đoán/liệu trình/đơn thuốc của lần khám TRƯỚC ĐÂY phải xuất hiện trong nội dung
    /// gửi cho AI tóm tắt, và Disclaimer phải luôn có giá trị cố định bất kể AI trả về gì.
    /// </summary>
    [Test]
    public async Task HandleAsync_PatientHasPastVisit_IncludesHistoryInPromptAndReturnsFixedDisclaimer()
    {
        var patientUser = User.Create("p1", "p1@test.com", "hash", "Patient");
        var dentistUser = User.Create("d1", "d1@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);

        var patient = Patient.Create("Bệnh nhân Test", new DateOnly(1990, 1, 1), "Nam", patientUser.Id);
        var dentist = Dentist.Create(dentistUser.Id, "BS. Test", "Nha khoa tổng quát", 5);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);

        var pastAppointment = Appointment.Create(
            patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMonths(-2), symptoms: "Đau răng hàm dưới");
        _db.Appointments.Add(pastAppointment);
        await _db.SaveChangesAsync();

        _db.Diagnoses.Add(Diagnosis.Create(pastAppointment.Id, "K02.1", "Sâu răng tiến triển", "Cần theo dõi thêm"));
        var prescription = Prescription.Create(pastAppointment.Id);
        _db.Prescriptions.Add(prescription);
        await _db.SaveChangesAsync();
        _db.Set<PrescriptionItem>().Add(PrescriptionItem.Create(
            prescription.Id, "Amoxicillin", "500mg", 10, "viên", "Uống sau ăn, ngày 2 lần"));
        await _db.SaveChangesAsync();

        var currentAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(currentAppointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(currentAppointment.Id);

        result.Summary.Should().Be("Tóm tắt test");
        result.Disclaimer.Should().Contain("AI tạo tự động");

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(),
            Arg.Is<string>(content =>
                content.Contains("Sâu răng tiến triển") &&
                content.Contains("Amoxicillin") &&
                content.Contains("Đau răng hàm dưới")),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Gọi HandleAsync lần 2 cho cùng lịch hẹn (không có lịch sử mới, không force) phải trả về
    /// đúng bản tóm tắt đã cache — KHÔNG gọi lại Gemini lần nữa, tránh tốn chi phí AI vô ích.
    /// </summary>
    [Test]
    public async Task HandleAsync_CalledTwiceWithoutNewHistory_ReturnsCachedSummaryWithoutCallingAiAgain()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p3", "d3");
        var pastAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        _db.Appointments.Add(pastAppointment);
        var currentAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(currentAppointment);
        await _db.SaveChangesAsync();

        var first = await _handler.HandleAsync(currentAppointment.Id);
        var second = await _handler.HandleAsync(currentAppointment.Id);

        first.FromCache.Should().BeFalse();
        second.FromCache.Should().BeTrue();
        second.Summary.Should().Be(first.Summary);

        await _aiChatService.Received(1).SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>forceRegenerate = true phải bỏ qua cache và gọi lại Gemini dù chưa có lịch sử mới.</summary>
    [Test]
    public async Task HandleAsync_ForceRegenerate_CallsAiAgainEvenWithoutNewHistory()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p4", "d4");
        var pastAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        _db.Appointments.Add(pastAppointment);
        var currentAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(currentAppointment);
        await _db.SaveChangesAsync();

        await _handler.HandleAsync(currentAppointment.Id);
        var forced = await _handler.HandleAsync(currentAppointment.Id, forceRegenerate: true);

        forced.FromCache.Should().BeFalse();
        await _aiChatService.Received(2).SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Có thêm lịch khám mới kể từ lần tóm tắt trước → cache phải bị coi là cũ và tạo lại.</summary>
    [Test]
    public async Task HandleAsync_NewPastAppointmentAddedSinceLastSummary_RegeneratesInsteadOfUsingCache()
    {
        var (patient, dentist) = await SeedPatientAndDentistAsync("p5", "d5");
        var pastAppointment1 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMonths(-2));
        _db.Appointments.Add(pastAppointment1);
        var currentAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(currentAppointment);
        await _db.SaveChangesAsync();

        await _handler.HandleAsync(currentAppointment.Id);

        var pastAppointment2 = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow.AddMonths(-1));
        _db.Appointments.Add(pastAppointment2);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(currentAppointment.Id);

        result.FromCache.Should().BeFalse();
        await _aiChatService.Received(2).SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private async Task<(Patient patient, Dentist dentist)> SeedPatientAndDentistAsync(
        string patientUsername, string dentistUsername)
    {
        var patientUser = User.Create(patientUsername, $"{patientUsername}@test.com", "hash", "Patient");
        var dentistUser = User.Create(dentistUsername, $"{dentistUsername}@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);

        var patient = Patient.Create("Bệnh nhân Test", new DateOnly(1990, 1, 1), "Nam", patientUser.Id);
        var dentist = Dentist.Create(dentistUser.Id, "BS. Test", "Nha khoa tổng quát", 5);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);
        await _db.SaveChangesAsync();

        return (patient, dentist);
    }

    [Test]
    public async Task HandleAsync_PatientHasNoPastVisit_ReturnsCannedMessageWithoutCallingAi()
    {
        var patientUser = User.Create("p2", "p2@test.com", "hash", "Patient");
        var dentistUser = User.Create("d2", "d2@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);

        var patient = Patient.Create("Bệnh nhân Mới", new DateOnly(1995, 1, 1), "Nữ", patientUser.Id);
        var dentist = Dentist.Create(dentistUser.Id, "BS. Test 2", "Nha khoa tổng quát", 3);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);

        var onlyAppointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(onlyAppointment);
        await _db.SaveChangesAsync();

        var result = await _handler.HandleAsync(onlyAppointment.Id);

        result.Summary.Should().Contain("chưa có lịch sử khám");
        await _aiChatService.DidNotReceive().SummarizeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
