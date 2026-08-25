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
    private IPromotionRepository _promotionRepo = null!;

    private GetBillablePlansHandler _getBillablePlansHandler = null!;
    private IssueInvoiceHandler _issueHandler = null!;
    private GetPendingInvoicesHandler _getPendingHandler = null!;
    private GetOutstandingInvoicesHandler _getOutstandingHandler = null!;
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
        var invoiceRepository = new InvoiceRepository(_db);
        var paymentTransactionRepository = new PaymentTransactionRepository(_db);
        _invoiceQuery = new InvoiceQueryHelper(invoiceRepository, new TreatmentPlanRepository(_db));
        _confirmationService = new PaymentConfirmationService(
            invoiceRepository, paymentTransactionRepository, _db, _notificationService, _userRepo, _invoiceQuery);

        _promotionRepo = Substitute.For<IPromotionRepository>();
        _getBillablePlansHandler = new GetBillablePlansHandler(invoiceRepository, _invoiceQuery);
        _issueHandler = new IssueInvoiceHandler(invoiceRepository, _promotionRepo, _db, _notificationService, _invoiceQuery);
        _getPendingHandler = new GetPendingInvoicesHandler(invoiceRepository);
        _getOutstandingHandler = new GetOutstandingInvoicesHandler(invoiceRepository);
        _confirmPaymentHandler = new ConfirmInvoicePaymentHandler(invoiceRepository, _db, _confirmationService, _invoiceQuery);
        _collectRemainingHandler = new CollectRemainingInvoiceHandler(invoiceRepository, _db, _invoiceQuery);
        _getHistoryHandler = new GetInvoiceHistoryHandler(invoiceRepository);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<(Appointment appointment, Patient patient, Guid patientUserId)> SeedPendingPaymentAppointmentAsync()
    {
        var patientUser = User.Create("inv-p", $"inv-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("inv-d", $"inv-d-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
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
        return (appointment, patient, patientUser.Id);
    }

    // "Trám răng" luôn có UnitPrice 500_000đ (Quantity=1) — khi paymentType="deposit", AmountCollected của
    // dòng phải được set = deposit, nếu không Invoice.Issue() mặc định thu toàn bộ dòng (AmountCollected=null
    // → full lineTotal), khiến mọi test "đặt cọc" vô tình trở thành thanh toán toàn bộ.
    private static IssueInvoiceCommand MakeIssueCommand(Guid appointmentId, string? paymentType = null, decimal deposit = 0) => new(
        appointmentId,
        new List<IssueInvoiceItemRequest>
        {
            new("Trám răng", 1, 500_000m, AmountCollected: paymentType == "deposit" ? deposit : null)
        },
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
        var dentistUser = User.Create("inv1", $"inv1-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
        _db.Users.Add(dentistUser);
        var employee = Employee.Create(dentistUser.Id, $"DT-{Guid.NewGuid():N}");
        employee.User = dentistUser;
        var dentist = DentistProfile.Create(employee.Id, "Nha khoa tổng quát", "N/A", 5);
        dentist.Employee = employee;
        var patient = Patient.Create(Guid.Empty, new DateOnly(1990, 1, 1), "Nam");
        _db.Employees.Add(employee);
        _db.DentistProfiles.Add(dentist);
        _db.Patients.Add(patient);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Nhánh xuất hóa đơn thường (không gắn liệu trình) cho phép nhiều hóa đơn/buổi (ví dụ hóa
    /// đơn đặt cọc + hóa đơn thu phần còn lại cho các dịch vụ không thuộc liệu trình nào) — chỉ chặn
    /// vượt tổng tiền khi dòng hóa đơn có gắn TreatmentPlanId (xem test khác ở dưới).</summary>
    [Test]
    public async Task IssueAsync_AppointmentAlreadyHasInvoice_AllowsSecondAdHocInvoice()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        var second = await _issueHandler.Handle(MakeIssueCommand(appointment.Id), CancellationToken.None);

        second.Should().NotBeNull();
        (await _db.Invoices.Where(i => i.AppointmentId == appointment.Id).CountAsync()).Should().Be(2);
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
        var dentist = await _db.DentistProfiles.FirstAsync();
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
        var dentist = await _db.DentistProfiles.FirstAsync();
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

    /// <summary>
    /// Công nợ liệu trình: liệu trình đã thu một phần mà còn thiếu — credit đúng số đã thu (deposit)
    /// từ hóa đơn ĐÃ Paid. GetPlanBilledMapAsync dùng để biết phần đã gắn hóa đơn (không xuất trùng);
    /// ở đây cả liệu trình đã nằm trọn trên một hóa đơn nên phần chưa gắn hóa đơn = 0.
    /// </summary>
    [Test]
    public async Task GetPlanPaidMapAsync_PlanFullyBilled_CreditsDepositWithZeroUnbilled()
    {
        var (appointmentA, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.DentistProfiles.FirstAsync();
        var service = Service.Create("Trồng Implant", 15_000_000m, 90, "Cấy ghép implant");
        _db.Services.Add(service);
        var inProgressPlan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        inProgressPlan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(inProgressPlan);
        await _db.SaveChangesAsync();

        // GetPlanPaidMapAsync chỉ credit công nợ từ hóa đơn ĐÃ Paid (chưa Paid thì chưa tính là "đã thu")
        // — phải MarkAsPaid thật, không chỉ Issue, mới khớp đúng luồng nghiệp vụ thật.
        var deposit = service.Price / 3;
        var partialInvoice = Invoice.Issue(
            appointmentA.Id, "INV-OUTSTANDING-TEST",
            [("Đặt cọc liệu trình", 1, service.Price, inProgressPlan.Id, deposit)],
            discount: 0, PaymentMethod.Cash);
        partialInvoice.MarkAsPaid(PaymentMethod.Cash);
        _db.Invoices.Add(partialInvoice);
        await _db.SaveChangesAsync();

        var planIds = new List<Guid> { inProgressPlan.Id };
        var paidMap = await _invoiceQuery.GetPlanPaidMapAsync(planIds, CancellationToken.None);
        var billedMap = await _invoiceQuery.GetPlanBilledMapAsync(planIds, CancellationToken.None);

        paidMap.GetValueOrDefault(inProgressPlan.Id).Should().Be(deposit);
        (service.Price - paidMap.GetValueOrDefault(inProgressPlan.Id)).Should().Be(service.Price - deposit);
        // Toàn bộ 15tr đã nằm trên hóa đơn (dòng hóa đơn = trọn giá liệu trình) → không còn gì để
        // xuất hóa đơn nữa; phần thiếu 10tr là công nợ CỦA HÓA ĐƠN, không phải phần chờ xuất HĐ.
        (service.Price - billedMap.GetValueOrDefault(inProgressPlan.Id)).Should().Be(0);
    }

    /// <summary>
    /// Liệu trình trả góp: mới xuất hóa đơn đợt 1 (đã thu đủ đợt đó) thì phần chưa xuất hóa đơn mới là
    /// công nợ liệu trình — GetPlanBilledMapAsync phải phản ánh đúng số tiền còn phải xuất hóa đơn.
    /// </summary>
    [Test]
    public async Task GetPlanBilledMapAsync_PartiallyBilledPlan_ReportsUnbilledAmount()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.DentistProfiles.FirstAsync();
        var service = Service.Create("Niềng răng", 15_000_000m, 90, "Chỉnh nha");
        _db.Services.Add(service);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        // Đợt 1: xuất hóa đơn 5tr trong tổng 15tr và đã thu đủ đợt này → hóa đơn không còn nợ,
        // nhưng liệu trình còn 10tr chưa xuất hóa đơn.
        var firstInstallment = Invoice.Issue(
            appointment.Id, "INV-INSTALLMENT-1",
            [("Đợt 1 - Niềng răng", 1, 5_000_000m, plan.Id, null)],
            discount: 0, PaymentMethod.Cash);
        firstInstallment.MarkAsPaid(PaymentMethod.Cash);
        _db.Invoices.Add(firstInstallment);
        await _db.SaveChangesAsync();

        var planIds = new List<Guid> { plan.Id };
        var paidMap = await _invoiceQuery.GetPlanPaidMapAsync(planIds, CancellationToken.None);
        var billedMap = await _invoiceQuery.GetPlanBilledMapAsync(planIds, CancellationToken.None);

        paidMap.GetValueOrDefault(plan.Id).Should().Be(5_000_000m);
        (service.Price - paidMap.GetValueOrDefault(plan.Id)).Should().Be(10_000_000m);
        (service.Price - billedMap.GetValueOrDefault(plan.Id)).Should().Be(10_000_000m);
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

    /// <summary>
    /// Thu đủ tiền KHÔNG còn làm liệu trình chuyển sang Completed: "hoàn thành" là kết luận chuyên môn,
    /// chỉ đạt khi mọi bước quy trình đã ghi nhận 100% (bệnh nhân trả trước cả liệu trình niềng răng
    /// vẫn đang điều trị dở).
    /// </summary>
    [Test]
    public async Task ConfirmPaymentAsync_PlanInstallmentFullyPaid_KeepsTreatmentPlanInProgress()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.DentistProfiles.FirstAsync();
        var service = Service.Create("Trồng Implant", 500_000m, 90, "Cấy ghép implant");
        _db.Services.Add(service);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, null, service.Id, service.Price, 1);
        plan.SetStatus(TreatmentPlanStatus.InProgress);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();
        var invoice = await _issueHandler.Handle(
            MakeIssueCommand(appointment.Id) with { TreatmentPlanId = plan.Id }, CancellationToken.None);

        await _confirmPaymentHandler.Handle(new ConfirmInvoicePaymentCommand(invoice.Id, null), CancellationToken.None);

        (await _db.TreatmentPlans.SingleAsync(p => p.Id == plan.Id)).Status.Should().Be(TreatmentPlanStatus.InProgress);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Khuyến mãi — khớp theo ServiceId của liệu trình từng dòng, không phải so
    // tên chuỗi hay giá gốc dịch vụ (Service.Price). Đây là chỗ trước đây bị lỗi:
    // giảm giá bị áp lên TOÀN BỘ hóa đơn hễ có promotionId, không xét dòng nào
    // thực sự thuộc dịch vụ được khuyến mãi.
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>Chỉ dòng thuộc ĐÚNG dịch vụ được khuyến mãi mới bị giảm giá — dòng dịch vụ khác trong
    /// cùng hóa đơn không liên quan không được giảm theo (lỗi cũ: giảm cả hóa đơn).</summary>
    [Test]
    public async Task IssueAsync_PromotionMatchesOneService_DiscountsOnlyThatLine()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.DentistProfiles.FirstAsync();

        // Giá gốc dịch vụ (5tr) KHÁC với giá option thực tế dùng trên liệu trình (2tr, vd option
        // "Titan") — để khẳng định khuyến mãi bám theo UnitPrice của dòng (giá option đã chọn),
        // không phải Service.Price.
        var promotedService = Service.Create("Bọc răng sứ", 5_000_000m, 60, "Bọc răng sứ thẩm mỹ");
        var otherService = Service.Create("Khám tổng quát", 300_000m, 30, "Khám tổng quát");
        _db.Services.AddRange(promotedService, otherService);
        var promotedPlan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, promotedService.Id, 2_000_000m, 1);
        var otherPlan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, otherService.Id, otherService.Price, 1);
        _db.TreatmentPlans.AddRange(promotedPlan, otherPlan);
        await _db.SaveChangesAsync();

        var promotion = Promotion.Create(
            "GIAM20", "Giảm 20% bọc răng sứ", null, "Percentage", 20m,
            new List<Guid> { promotedService.Id },
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), true);
        _promotionRepo.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new IssueInvoiceCommand(
            appointment.Id,
            new List<IssueInvoiceItemRequest>
            {
                // AmountCollected khớp đúng phần đã trừ khuyến mãi của từng dòng — nếu không, "thu ngay"
                // mặc định = thành tiền gốc sẽ vượt tổng hóa đơn đã giảm giá và bị chặn ở bước khác.
                new("Bọc răng sứ - Titan", 1, 2_000_000m, promotedPlan.Id, AmountCollected: 1_600_000m),
                new("Khám tổng quát", 1, 300_000m, otherPlan.Id, AmountCollected: 300_000m),
            },
            Discount: 0, PaymentMethod: "cash", PaymentType: null, DepositAmount: 0,
            Notes: null, ParentInvoiceId: null, TreatmentPlanId: null, PromotionId: promotion.Id);

        var result = await _issueHandler.Handle(command, CancellationToken.None);

        result.Subtotal.Should().Be(2_300_000m);
        result.Discount.Should().Be(400_000m); // 20% của 2.000.000 (dòng được khuyến mãi), KHÔNG PHẢI 20% của 2.300.000
        result.TotalAmount.Should().Be(1_900_000m);
    }

    /// <summary>Khuyến mãi có ServiceIds rỗng nghĩa là áp dụng cho TẤT CẢ dịch vụ — vẫn phải giảm trên
    /// toàn bộ hóa đơn như quy ước cũ.</summary>
    [Test]
    public async Task IssueAsync_PromotionWithEmptyServiceIds_AppliesToWholeSubtotal()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.DentistProfiles.FirstAsync();
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        _db.Services.Add(service);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, service.Price, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        var promotion = Promotion.Create(
            "GIAMTATCA", "Giảm cho tất cả dịch vụ", null, "Percentage", 10m,
            new List<Guid>(),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), true);
        _promotionRepo.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new IssueInvoiceCommand(
            appointment.Id,
            new List<IssueInvoiceItemRequest> { new("Trám răng", 1, 500_000m, plan.Id, AmountCollected: 450_000m) },
            Discount: 0, PaymentMethod: "cash", PaymentType: null, DepositAmount: 0,
            Notes: null, ParentInvoiceId: null, TreatmentPlanId: null, PromotionId: promotion.Id);

        var result = await _issueHandler.Handle(command, CancellationToken.None);

        result.Discount.Should().Be(50_000m);
        result.TotalAmount.Should().Be(450_000m);
    }

    /// <summary>Khuyến mãi không khớp dịch vụ nào trong hóa đơn phải bị từ chối thay vì âm thầm giảm 0đ
    /// hoặc (lỗi cũ) giảm nhầm cả hóa đơn.</summary>
    [Test]
    public async Task IssueAsync_PromotionMatchesNoLine_ThrowsValidationException()
    {
        var (appointment, patient, _) = await SeedPendingPaymentAppointmentAsync();
        var dentist = await _db.DentistProfiles.FirstAsync();
        var service = Service.Create("Trám răng", 500_000m, 30, "Trám răng thẩm mỹ");
        var unrelatedService = Service.Create("Niềng răng", 20_000_000m, 60, "Chỉnh nha");
        _db.Services.AddRange(service, unrelatedService);
        var plan = TreatmentPlan.Create(patient.Id, dentist.Id, appointment.Id, service.Id, service.Price, 1);
        _db.TreatmentPlans.Add(plan);
        await _db.SaveChangesAsync();

        var promotion = Promotion.Create(
            "GIAMNIENG", "Giảm niềng răng", null, "Percentage", 10m,
            new List<Guid> { unrelatedService.Id },
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), true);
        _promotionRepo.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new IssueInvoiceCommand(
            appointment.Id,
            new List<IssueInvoiceItemRequest> { new("Trám răng", 1, 500_000m, plan.Id) },
            Discount: 0, PaymentMethod: "cash", PaymentType: null, DepositAmount: 0,
            Notes: null, ParentInvoiceId: null, TreatmentPlanId: null, PromotionId: promotion.Id);

        Func<Task> act = () => _issueHandler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
