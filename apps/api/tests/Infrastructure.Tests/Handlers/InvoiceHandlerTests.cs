using DentalClinic.API.Application.DTOs.Invoices;
using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
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
public class InvoiceHandlerTests
{
    private AppDbContext _db = null!;
    private INotificationService _notificationService = null!;
    private IUserRepository _userRepo = null!;
    private InvoiceQueryHelper _invoiceQuery = null!;
    private IPaymentConfirmationService _confirmationService = null!;

    private GetBillablePlansHandler _getBillablePlansHandler = null!;
    private IssueInvoiceHandler _issueHandler = null!;
    private GetPendingInvoicesHandler _getPendingHandler = null!;
    private GetOutstandingInvoicesHandler _getOutstandingHandler = null!;
    private GetOutstandingPlansHandler _getOutstandingPlansHandler = null!;
    private ConfirmInvoicePaymentHandler _confirmPaymentHandler = null!;
    private CollectRemainingInvoiceHandler _collectRemainingHandler = null!;
    private GetInvoiceHistoryHandler _getHistoryHandler = null!;

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
        _confirmationService = new PaymentConfirmationService(_db, _notificationService, _userRepo, _invoiceQuery);

        _getBillablePlansHandler = new GetBillablePlansHandler(_db, _invoiceQuery);
        _issueHandler = new IssueInvoiceHandler(_db, _notificationService, _invoiceQuery);
        _getPendingHandler = new GetPendingInvoicesHandler(_invoiceQuery);
        _getOutstandingHandler = new GetOutstandingInvoicesHandler(_invoiceQuery);
        _getOutstandingPlansHandler = new GetOutstandingPlansHandler(_db, _invoiceQuery);
        _confirmPaymentHandler = new ConfirmInvoicePaymentHandler(_db, _confirmationService, _invoiceQuery);
        _collectRemainingHandler = new CollectRemainingInvoiceHandler(_db, _invoiceQuery);
        _getHistoryHandler = new GetInvoiceHistoryHandler(_invoiceQuery);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Appointment appointment, Patient patient, Guid patientUserId)> SeedPendingPaymentAppointmentAsync()
    {
        var patientUser = User.Create("inv-p", $"inv-p-{Guid.NewGuid()}@test.com", "hash", "Patient");
        var dentistUser = User.Create("inv-d", $"inv-d-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create(patientUser.Id, new DateOnly(1990, 1, 1), "Nam");
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        appointment.EndTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return (appointment, patient, patientUser.Id);
    }

    private static IssueInvoiceCommand MakeIssueCommand(Guid appointmentId, string? paymentType = null, decimal deposit = 0) => new(
        appointmentId,
        new List<IssueInvoiceItemRequest> { new("Trám răng", 1, 500_000m) },
        Discount: 0,
        PaymentMethod: "cash",
        PaymentType: paymentType,
        DepositAmount: deposit,
        Notes: null,
        ParentInvoiceId: null,
        TreatmentPlanId: null);

    /// <summary>Không có lịch hẹn nào chờ thanh toán phải trả về danh sách rỗng.</summary>
    [Test]
    public async Task GetBillablePlansAsync_NoPendingPaymentAppointments_ReturnsEmpty()
    {
        var result = await _getBillablePlansHandler.Handle(new GetBillablePlansQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    /// <summary>Xuất hóa đơn cho lịch hẹn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task IssueAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _issueHandler.Handle(MakeIssueCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Chỉ được xuất hóa đơn cho lịch hẹn đã ở trạng thái chờ thanh toán (PendingPayment).</summary>
    [Test]
    public async Task IssueAsync_AppointmentNotPendingPayment_ThrowsValidationException()
    {
        var dentistUser = User.Create("inv1", $"inv1-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.Add(dentistUser);
        var dentist = Dentist.Create(dentistUser.Id, "Nha khoa tổng quát", 5);
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Dentists.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Lịch hẹn đã có hóa đơn rồi thì không cho xuất thêm.</summary>
    [Test]
    public async Task IssueAsync_AppointmentAlreadyHasInvoice_ThrowsConflictException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        Func<Task> act = () => _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Đặt cọc với số tiền vượt quá tổng hóa đơn phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_DepositAmountExceedsTotal_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();

        Func<Task> act = () => _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 10_000_000m), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Xuất hóa đơn thanh toán toàn bộ hợp lệ phải lưu đúng tổng tiền và báo cho bệnh nhân có tài khoản.</summary>
    [Test]
    public async Task IssueAsync_ValidFullPayment_CreatesInvoiceAndNotifiesPatient()
    {
        var (appointment, _, patientUserId) = await SeedPendingPaymentAppointmentAsync();

        var result = await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        result.TotalAmount.Should().Be(500_000m);
        result.Status.Should().Be(PaymentStatus.Unpaid.ToString());
        await _notificationService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.UserId == patientUserId), Arg.Any<CancellationToken>());
    }

    /// <summary>Tab "Chờ thanh toán" chỉ được trả về các hóa đơn chưa thanh toán.</summary>
    [Test]
    public async Task GetPendingAsync_ReturnsOnlyUnpaidInvoices()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        var result = await _getPendingHandler.Handle(new GetPendingInvoicesQuery(), CancellationToken.None);

        result.Should().ContainSingle(i => i.Id == invoice.Id);
    }

    /// <summary>Tab "Công nợ" chỉ trả về hóa đơn đặt cọc chưa thu đủ, chưa tất toán.</summary>
    [Test]
    public async Task GetOutstandingAsync_ReturnsOnlyUnsettledDepositInvoices()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 200_000m), CancellationToken.None);

        var result = await _getOutstandingHandler.Handle(new GetOutstandingInvoicesQuery(), CancellationToken.None);

        result.Should().ContainSingle(i => i.Id == invoice.Id);
        result[0].RemainingAmount.Should().Be(300_000m);
    }

    /// <summary>Xác nhận thanh toán cho hóa đơn không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _confirmPaymentHandler.Handle(
            new ConfirmInvoicePaymentCommand(Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Hóa đơn đã thanh toán rồi thì không cho xác nhận lại lần nữa.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_AlreadyPaid_ThrowsConflictException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);
        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(invoice.Id, null), CancellationToken.None);

        Func<Task> act = () => _confirmPaymentHandler.Handle(
            new ConfirmInvoicePaymentCommand(invoice.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Xác nhận thanh toán hợp lệ phải đánh dấu hóa đơn Paid, hoàn tất lịch hẹn và báo cho bệnh nhân + staff.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_ValidRequest_MarksPaidAndCompletesAppointment()
    {
        var staffUserId = Guid.NewGuid();
        _userRepo.GetUserIdsByRoleAsync("Staff", Arg.Any<CancellationToken>()).Returns(new List<Guid> { staffUserId });
        var (appointment, _, patientUserId) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        var result = await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(invoice.Id, null), CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Paid.ToString());
        (await _db.Appointments.SingleAsync(a => a.Id == appointment.Id)).Status.Should().Be(AppointmentStatus.Completed);
        await _notificationService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.UserId == patientUserId && r.Title == "Thanh toán thành công"),
            Arg.Any<CancellationToken>());
        await _notificationService.Received(1).CreateForMultipleUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(staffUserId)), Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Tái hiện báo cáo lỗi 500 "Đã xảy ra lỗi hệ thống." khi bấm "Xác nhận thủ công" cho hóa đơn đang chờ thanh
    /// toán online (đã có sẵn 1 giao dịch PayOS Pending do admin_website tự tạo QR khi mở hóa đơn) — xác nhận
    /// ConfirmInvoicePaymentAsync có tự đóng giao dịch Pending đó mà không throw.
    /// </summary>
    [Test]
    public async Task ConfirmPaymentAsync_InvoiceHasPendingPaymentTransaction_MarksPaidAndClosesTransaction()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var command = MakeIssueCommand(appointment.Id) with { PaymentMethod = "app" };
        var invoice = await _issueHandler.Handle(command, CancellationToken.None);

        var txn = PaymentTransaction.Create(
            invoice.Id, PaymentGateway.PayOS, "ORDER123", invoice.DepositAmount,
            "https://pay.example/checkout", "00020101...", "{}", DateTimeOffset.UtcNow.AddMinutes(15));
        _db.PaymentTransactions.Add(txn);
        await _db.SaveChangesAsync();

        var result = await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(invoice.Id, null), CancellationToken.None);

        result.Status.Should().Be(PaymentStatus.Paid.ToString());
        (await _db.PaymentTransactions.SingleAsync(t => t.Id == txn.Id)).Status.Should().Be(TransactionStatus.Failed);
    }

    /// <summary>Bắt đầu thu phần còn lại cho hóa đơn không có công nợ phải bị từ chối.</summary>
    [Test]
    public async Task CollectRemainingAsync_NoRemainingDebt_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None); // thanh toán toàn bộ, không còn nợ

        Func<Task> act = () => _collectRemainingHandler.Handle(new CollectRemainingInvoiceCommand(invoice.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Bắt đầu thu phần còn lại hợp lệ phải đánh dấu hóa đơn đang trong quy trình thu nốt.</summary>
    [Test]
    public async Task CollectRemainingAsync_ValidRequest_MarksCollectingRemaining()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 200_000m), CancellationToken.None);

        var result = await _collectRemainingHandler.Handle(new CollectRemainingInvoiceCommand(invoice.Id), CancellationToken.None);

        result.CollectingRemaining.Should().BeTrue();
    }

    /// <summary>Xuất hóa đơn với danh sách dịch vụ rỗng phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_EmptyItems_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var command = MakeIssueCommand(appointment.Id) with { Items = new List<IssueInvoiceItemRequest>() };

        Func<Task> act = () => _issueHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Phương thức thanh toán không hợp lệ phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_InvalidPaymentMethod_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var command = MakeIssueCommand(appointment.Id) with { PaymentMethod = "bitcoin" };

        Func<Task> act = () => _issueHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Xuất hóa đơn đặt cọc hợp lệ phải lưu đúng số tiền đặt cọc và số tiền còn lại.</summary>
    [Test]
    public async Task IssueAsync_ValidDepositPayment_SavesCorrectDepositAndRemaining()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();

        var result = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 150_000m), CancellationToken.None);

        result.DepositAmount.Should().Be(150_000m);
        result.RemainingAmount.Should().Be(350_000m);
        result.PaymentType.Should().Be(PaymentType.Deposit.ToString());
    }

    /// <summary>Tab "Lịch sử" chỉ trả về hóa đơn đã thanh toán, mới thanh toán gần nhất trước.</summary>
    [Test]
    public async Task GetHistoryAsync_ReturnsOnlyPaidInvoicesOrderedByPaymentDateDescending()
    {
        var (appointment1, _, _) = await SeedPendingPaymentAppointmentAsync();
        var (appointment2, _, _) = await SeedPendingPaymentAppointmentAsync();
        var pending = await _issueHandler.Handle(MakeIssueCommand(appointment1.Id), CancellationToken.None);
        var paid = await _issueHandler.Handle(MakeIssueCommand(appointment2.Id), CancellationToken.None);
        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(paid.Id, null), CancellationToken.None);

        var result = await _getHistoryHandler.Handle(new GetInvoiceHistoryQuery(), CancellationToken.None);

        result.Should().ContainSingle(i => i.Id == paid.Id);
        result.Should().NotContain(i => i.Id == pending.Id);
    }

    /// <summary>Thu một đợt của liệu trình điều trị (installment) cho liệu trình không ở trạng thái đang điều trị phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_PlanInstallment_PlanNotInProgress_ThrowsValidationException()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.Dentists.FirstAsync();
        var service = Service.Create("Niềng răng", 20_000_000m, 60, "Chỉnh nha");
        _db.Services.Add(service);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.Completed); // không còn đang điều trị
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        var command = MakeIssueCommand(appointment.Id) with { TreatmentPlanId = plan.Id };

        Func<Task> act = () => _issueHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Thu một đợt của liệu trình điều trị hợp lệ phải tạo hóa đơn gắn đúng TreatmentPlanId.</summary>
    [Test]
    public async Task IssueAsync_PlanInstallment_ValidRequest_CreatesInstallmentInvoice()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.Dentists.FirstAsync();
        var service = Service.Create("Niềng răng", 20_000_000m, 60, "Chỉnh nha");
        _db.Services.Add(service);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        var command = MakeIssueCommand(appointment.Id) with { TreatmentPlanId = plan.Id };

        var result = await _issueHandler.Handle(command, CancellationToken.None);

        result.TotalAmount.Should().Be(500_000m);
        (await _db.Invoices.SingleAsync(i => i.Id == result.Id)).TreatmentPlanId.Should().Be(plan.Id);
    }

    /// <summary>Thu phần còn lại cho hóa đơn gốc đã tất toán rồi phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_RemainingCollection_ParentAlreadySettled_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 200_000m), CancellationToken.None);
        var parentEntity = await _db.Invoices.SingleAsync(i => i.Id == parent.Id);
        parentEntity.Settle();
        await _db.SaveChangesAsync();
        var command = MakeIssueCommand(appointment.Id) with { ParentInvoiceId = parent.Id };

        Func<Task> act = () => _issueHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Thu phần còn lại hợp lệ phải tạo hóa đơn con gắn đúng ParentInvoiceId với số tiền bằng phần còn lại.</summary>
    [Test]
    public async Task IssueAsync_RemainingCollection_ValidRequest_CreatesChildInvoiceWithRemainingAmount()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 200_000m), CancellationToken.None);
        var command = MakeIssueCommand(appointment.Id) with { ParentInvoiceId = parent.Id };

        var result = await _issueHandler.Handle(command, CancellationToken.None);

        result.ParentInvoiceId.Should().Be(parent.Id);
        result.TotalAmount.Should().Be(300_000m); // 500_000 - 200_000 đã đặt cọc
    }

    /// <summary>Đã có hóa đơn con thu phần còn lại rồi thì không cho tạo thêm hóa đơn con thứ hai.</summary>
    [Test]
    public async Task IssueAsync_RemainingCollection_AlreadyHasChildInvoice_ThrowsConflictException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 200_000m), CancellationToken.None);
        await _issueHandler.Handle(MakeIssueCommand(appointment.Id) with { ParentInvoiceId = parent.Id }, CancellationToken.None);

        Func<Task> act = () => _issueHandler.Handle(
            MakeIssueCommand(appointment.Id) with { ParentInvoiceId = parent.Id }, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Tab "Công nợ" — liệu trình chỉ trả về liệu trình đang điều trị còn nợ (đã trừ số đã thu).</summary>
    [Test]
    public async Task GetOutstandingPlansAsync_ReturnsInProgressPlansWithRemainingBalanceOnly()
    {
        var (_, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.Dentists.FirstAsync();
        var service = Service.Create("Trồng Implant", 15_000_000m, 90, "Cấy ghép implant");
        _db.Services.Add(service);
        var inProgressPlan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        inProgressPlan.SetStatus(TreatmentPlanStatus.InProgress);
        var completedPlan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        completedPlan.SetStatus(TreatmentPlanStatus.Completed);
        _db.TreatmentPlans.AddRange(inProgressPlan, completedPlan);
        await _db.SaveChangesAsync();

        var result = await _getOutstandingPlansHandler.Handle(new GetOutstandingPlansQuery(), CancellationToken.None);

        result.Should().ContainSingle(p => p.TreatmentPlanId == inProgressPlan.Id);
        result[0].RemainingAmount.Should().Be(service.Price);
    }

    /// <summary>Xác nhận thanh toán cho hóa đơn thu phần còn lại phải tất toán (Settle) hóa đơn gốc.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_RemainingCollectionInvoice_SettlesParentInvoice()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id, "deposit", deposit: 200_000m), CancellationToken.None);
        var child = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id) with { ParentInvoiceId = parent.Id }, CancellationToken.None);

        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(child.Id, null), CancellationToken.None);

        (await _db.Invoices.SingleAsync(i => i.Id == parent.Id)).IsSettled.Should().BeTrue();
    }

    /// <summary>Xác nhận thanh toán đủ số tiền của liệu trình (installment cuối) phải chuyển liệu trình sang Completed.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_PlanInstallmentFullyPaid_CompletesTreatmentPlan()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.Dentists.FirstAsync();
        var service = Service.Create("Trồng Implant", 500_000m, 90, "Cấy ghép implant");
        _db.Services.Add(service);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        var invoice = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id) with { TreatmentPlanId = plan.Id }, CancellationToken.None);

        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(invoice.Id, null), CancellationToken.None);

        (await _db.TreatmentPlans.SingleAsync(p => p.Id == plan.Id)).Status.Should().Be(TreatmentPlanStatus.Completed);
    }
}
