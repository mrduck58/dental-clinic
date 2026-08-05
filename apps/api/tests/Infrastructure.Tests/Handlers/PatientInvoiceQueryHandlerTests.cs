using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Application.UseCases.Invoices;
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

/// <summary>Kiểm thử GetPaidInvoicesByPatientHandler và GetPendingInvoicesByPatientHandler — hai handler
/// dùng trên mobile app để bệnh nhân xem hóa đơn của chính mình, lọc đúng theo PatientId và trạng thái thanh toán.</summary>
[TestFixture]
public class PatientInvoiceQueryHandlerTests
{
    private AppDbContext _db = null!;
    private INotificationService _notificationService = null!;
    private IUserRepository _userRepo = null!;
    private InvoiceQueryHelper _invoiceQuery = null!;
    private IssueInvoiceHandler _issueHandler = null!;
    private ConfirmInvoicePaymentHandler _confirmPaymentHandler = null!;
    private GetPaidInvoicesByPatientHandler _getPaidHandler = null!;
    private GetPendingInvoicesByPatientHandler _getPendingHandler = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _notificationService = Substitute.For<INotificationService>();
        _userRepo = Substitute.For<IUserRepository>();
        _userRepo.GetUserIdsByRoleAsync("Staff", Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        _invoiceQuery = new InvoiceQueryHelper(_db, new TreatmentPlanRepository(_db));
        var confirmationService = new PaymentConfirmationService(_db, _notificationService, _userRepo, _invoiceQuery);

        _issueHandler = new IssueInvoiceHandler(_db, _notificationService, _invoiceQuery);
        _confirmPaymentHandler = new ConfirmInvoicePaymentHandler(_db, confirmationService, _invoiceQuery);
        _getPaidHandler = new GetPaidInvoicesByPatientHandler(_invoiceQuery);
        _getPendingHandler = new GetPendingInvoicesByPatientHandler(_invoiceQuery);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Appointment appointment, Patient patient)> SeedPendingPaymentAppointmentAsync()
    {
        var patientUser = User.Create("pinv-p", $"pinv-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("pinv-d", $"pinv-d-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        _db.Patients.Add(patient);
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        appointment.EndTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return (appointment, patient);
    }

    private static IssueInvoiceCommand MakeIssueCommand(Guid appointmentId) => new(
        appointmentId,
        new List<IssueInvoiceItemRequest> { new("Trám răng", 1, 500_000m) },
        Discount: 0,
        PaymentMethod: "cash",
        PaymentType: null,
        DepositAmount: 0,
        Notes: null,
        ParentInvoiceId: null,
        TreatmentPlanId: null);

    /// <summary>Bệnh nhân không có hóa đơn chưa thanh toán nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task GetPendingByPatientAsync_NoInvoices_ReturnsEmpty()
    {
        var (_, patient) = await SeedPendingPaymentAppointmentAsync();

        var result = await _getPendingHandler.Handle(new GetPendingInvoicesByPatientQuery(patient.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Chỉ trả về hóa đơn chưa thanh toán của đúng bệnh nhân được truy vấn, không lẫn hóa đơn
    /// của bệnh nhân khác hay hóa đơn đã thanh toán.</summary>
    [Test]
    public async Task GetPendingByPatientAsync_ReturnsOnlyUnpaidInvoicesOfThatPatient()
    {
        var (appointmentA, patientA) = await SeedPendingPaymentAppointmentAsync();
        var (appointmentB, _) = await SeedPendingPaymentAppointmentAsync();
        var pendingInvoiceA = await _issueHandler.Handle(MakeIssueCommand(appointmentA.Id), CancellationToken.None);
        var paidInvoiceA = await _issueHandler.Handle(MakeIssueCommand(appointmentA.Id), CancellationToken.None);
        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(paidInvoiceA.Id, null), CancellationToken.None);
        await _issueHandler.Handle(MakeIssueCommand(appointmentB.Id), CancellationToken.None); // hóa đơn của bệnh nhân khác

        var result = await _getPendingHandler.Handle(new GetPendingInvoicesByPatientQuery(patientA.Id), CancellationToken.None);

        result.Should().ContainSingle(i => i.Id == pendingInvoiceA.Id);
    }

    /// <summary>Danh sách hóa đơn chờ thanh toán phải sắp xếp theo ngày tạo tăng dần (cũ nhất trước).</summary>
    [Test]
    public async Task GetPendingByPatientAsync_OrdersByCreatedAtAscending()
    {
        var (appointment1, patient) = await SeedPendingPaymentAppointmentAsync();
        var (appointment2, _) = await SeedPendingPaymentAppointmentAsync();
        // Buổi hẹn thứ hai của cùng bệnh nhân, để hai hóa đơn cùng thuộc một patient.
        var secondAppointment = Appointment.Create(patient.Id, appointment2.DentistId, DateTimeOffset.UtcNow);
        secondAppointment.StartTreatment();
        secondAppointment.EndTreatment();
        _db.Appointments.Add(secondAppointment);
        await _db.SaveChangesAsync();

        var first = await _issueHandler.Handle(MakeIssueCommand(appointment1.Id), CancellationToken.None);
        var second = await _issueHandler.Handle(MakeIssueCommand(secondAppointment.Id), CancellationToken.None);

        var result = await _getPendingHandler.Handle(new GetPendingInvoicesByPatientQuery(patient.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(first.Id);
        result[1].Id.Should().Be(second.Id);
    }

    /// <summary>Bệnh nhân không có hóa đơn đã thanh toán nào phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task GetPaidByPatientAsync_NoPaidInvoices_ReturnsEmpty()
    {
        var (appointment, patient) = await SeedPendingPaymentAppointmentAsync();
        await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None); // vẫn chưa thanh toán

        var result = await _getPaidHandler.Handle(new GetPaidInvoicesByPatientQuery(patient.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Chỉ trả về hóa đơn đã thanh toán của đúng bệnh nhân được truy vấn, không lẫn hóa đơn
    /// chưa thanh toán hay hóa đơn của bệnh nhân khác.</summary>
    [Test]
    public async Task GetPaidByPatientAsync_ReturnsOnlyPaidInvoicesOfThatPatient()
    {
        var (appointmentA, patientA) = await SeedPendingPaymentAppointmentAsync();
        var (appointmentB, patientB) = await SeedPendingPaymentAppointmentAsync();
        var paidA = await _issueHandler.Handle(MakeIssueCommand(appointmentA.Id), CancellationToken.None);
        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(paidA.Id, null), CancellationToken.None);
        var paidB = await _issueHandler.Handle(MakeIssueCommand(appointmentB.Id), CancellationToken.None);
        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(paidB.Id, null), CancellationToken.None); // hóa đơn bệnh nhân khác

        var result = await _getPaidHandler.Handle(new GetPaidInvoicesByPatientQuery(patientA.Id), CancellationToken.None);

        result.Should().ContainSingle(i => i.Id == paidA.Id);
    }

    /// <summary>Danh sách hóa đơn đã thanh toán phải sắp xếp theo ngày thanh toán giảm dần (mới nhất trước).</summary>
    [Test]
    public async Task GetPaidByPatientAsync_OrdersByPaymentDateDescending()
    {
        var (appointment1, patient) = await SeedPendingPaymentAppointmentAsync();
        var (appointment2, _) = await SeedPendingPaymentAppointmentAsync();
        var secondAppointment = Appointment.Create(patient.Id, appointment2.DentistId, DateTimeOffset.UtcNow);
        secondAppointment.StartTreatment();
        secondAppointment.EndTreatment();
        _db.Appointments.Add(secondAppointment);
        await _db.SaveChangesAsync();

        var invoice1 = await _issueHandler.Handle(MakeIssueCommand(appointment1.Id), CancellationToken.None);
        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(invoice1.Id, null), CancellationToken.None);
        var invoice2 = await _issueHandler.Handle(MakeIssueCommand(secondAppointment.Id), CancellationToken.None);
        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(invoice2.Id, null), CancellationToken.None);

        var result = await _getPaidHandler.Handle(new GetPaidInvoicesByPatientQuery(patient.Id), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(invoice2.Id); // thanh toán sau -> đứng trước
        result[1].Id.Should().Be(invoice1.Id);
    }
}
