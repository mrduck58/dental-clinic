using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
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
public class InvoiceHandlerTests
{
    private AppDbContext _db = null!;
    private INotificationService _notificationService = null!;
    private IUserRepository _userRepo = null!;
    private InvoiceHandler _handler = null!;

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
        _handler = new InvoiceHandler(_db, _notificationService, _userRepo);
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

    private static IssueInvoiceRequest MakeIssueRequest(Guid appointmentId, string? paymentType = null, decimal deposit = 0) => new(
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
        var result = await _handler.GetBillablePlansAsync();

        result.Should().BeEmpty();
    }

    /// <summary>Xuất hóa đơn cho lịch hẹn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task IssueAsync_AppointmentNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.IssueAsync(MakeIssueRequest(Guid.NewGuid()));

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

        Func<Task> act = () => _handler.IssueAsync(MakeIssueRequest(appointment.Id));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Lịch hẹn đã có hóa đơn rồi thì không cho xuất thêm.</summary>
    [Test]
    public async Task IssueAsync_AppointmentAlreadyHasInvoice_ThrowsConflictException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        await _handler.IssueAsync(MakeIssueRequest(appointment.Id));

        Func<Task> act = () => _handler.IssueAsync(MakeIssueRequest(appointment.Id));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Đặt cọc với số tiền vượt quá tổng hóa đơn phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_DepositAmountExceedsTotal_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();

        Func<Task> act = () => _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 10_000_000m));

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Xuất hóa đơn thanh toán toàn bộ hợp lệ phải lưu đúng tổng tiền và báo cho bệnh nhân có tài khoản.</summary>
    [Test]
    public async Task IssueAsync_ValidFullPayment_CreatesInvoiceAndNotifiesPatient()
    {
        var (appointment, _, patientUserId) = await SeedPendingPaymentAppointmentAsync();

        var result = await _handler.IssueAsync(MakeIssueRequest(appointment.Id));

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
        var invoice = await _handler.IssueAsync(MakeIssueRequest(appointment.Id));

        var result = await _handler.GetPendingAsync();

        result.Should().ContainSingle(i => i.Id == invoice.Id);
    }

    /// <summary>Tab "Công nợ" chỉ trả về hóa đơn đặt cọc chưa thu đủ, chưa tất toán.</summary>
    [Test]
    public async Task GetOutstandingAsync_ReturnsOnlyUnsettledDepositInvoices()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 200_000m));

        var result = await _handler.GetOutstandingAsync();

        result.Should().ContainSingle(i => i.Id == invoice.Id);
        result[0].RemainingAmount.Should().Be(300_000m);
    }

    /// <summary>Xác nhận thanh toán cho hóa đơn không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.ConfirmPaymentAsync(Guid.NewGuid(), new ConfirmPaymentRequest(null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Hóa đơn đã thanh toán rồi thì không cho xác nhận lại lần nữa.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_AlreadyPaid_ThrowsConflictException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _handler.IssueAsync(MakeIssueRequest(appointment.Id));
        await _handler.ConfirmPaymentAsync(invoice.Id, new ConfirmPaymentRequest(null));

        Func<Task> act = () => _handler.ConfirmPaymentAsync(invoice.Id, new ConfirmPaymentRequest(null));

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Xác nhận thanh toán hợp lệ phải đánh dấu hóa đơn Paid, hoàn tất lịch hẹn và báo cho bệnh nhân + staff.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_ValidRequest_MarksPaidAndCompletesAppointment()
    {
        var staffUserId = Guid.NewGuid();
        _userRepo.GetUserIdsByRoleAsync("Staff", Arg.Any<CancellationToken>()).Returns(new List<Guid> { staffUserId });
        var (appointment, _, patientUserId) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _handler.IssueAsync(MakeIssueRequest(appointment.Id));

        var result = await _handler.ConfirmPaymentAsync(invoice.Id, new ConfirmPaymentRequest(null));

        result.Status.Should().Be(PaymentStatus.Paid.ToString());
        (await _db.Appointments.SingleAsync(a => a.Id == appointment.Id)).Status.Should().Be(AppointmentStatus.Completed);
        await _notificationService.Received(1).CreateAsync(
            Arg.Is<CreateNotificationRequest>(r => r.UserId == patientUserId && r.Title == "Thanh toán thành công"),
            Arg.Any<CancellationToken>());
        await _notificationService.Received(1).CreateForMultipleUsersAsync(
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(staffUserId)), Arg.Any<CreateNotificationRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Bắt đầu thu phần còn lại cho hóa đơn không có công nợ phải bị từ chối.</summary>
    [Test]
    public async Task CollectRemainingAsync_NoRemainingDebt_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _handler.IssueAsync(MakeIssueRequest(appointment.Id)); // thanh toán toàn bộ, không còn nợ

        Func<Task> act = () => _handler.CollectRemainingAsync(invoice.Id);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Bắt đầu thu phần còn lại hợp lệ phải đánh dấu hóa đơn đang trong quy trình thu nốt.</summary>
    [Test]
    public async Task CollectRemainingAsync_ValidRequest_MarksCollectingRemaining()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var invoice = await _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 200_000m));

        var result = await _handler.CollectRemainingAsync(invoice.Id);

        result.CollectingRemaining.Should().BeTrue();
    }

    /// <summary>Xuất hóa đơn với danh sách dịch vụ rỗng phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_EmptyItems_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var request = MakeIssueRequest(appointment.Id) with { Items = new List<IssueInvoiceItemRequest>() };

        Func<Task> act = () => _handler.IssueAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Phương thức thanh toán không hợp lệ phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_InvalidPaymentMethod_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var request = MakeIssueRequest(appointment.Id) with { PaymentMethod = "bitcoin" };

        Func<Task> act = () => _handler.IssueAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Xuất hóa đơn đặt cọc hợp lệ phải lưu đúng số tiền đặt cọc và số tiền còn lại.</summary>
    [Test]
    public async Task IssueAsync_ValidDepositPayment_SavesCorrectDepositAndRemaining()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();

        var result = await _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 150_000m));

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
        var pending = await _handler.IssueAsync(MakeIssueRequest(appointment1.Id));
        var paid = await _handler.IssueAsync(MakeIssueRequest(appointment2.Id));
        await _handler.ConfirmPaymentAsync(paid.Id, new ConfirmPaymentRequest(null));

        var result = await _handler.GetHistoryAsync();

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
        var request = MakeIssueRequest(appointment.Id) with { TreatmentPlanId = plan.Id };

        Func<Task> act = () => _handler.IssueAsync(request);

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
        var request = MakeIssueRequest(appointment.Id) with { TreatmentPlanId = plan.Id };

        var result = await _handler.IssueAsync(request);

        result.TotalAmount.Should().Be(500_000m);
        (await _db.Invoices.SingleAsync(i => i.Id == result.Id)).TreatmentPlanId.Should().Be(plan.Id);
    }

    /// <summary>Thu phần còn lại cho hóa đơn gốc đã tất toán rồi phải bị từ chối.</summary>
    [Test]
    public async Task IssueAsync_RemainingCollection_ParentAlreadySettled_ThrowsValidationException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 200_000m));
        var parentEntity = await _db.Invoices.SingleAsync(i => i.Id == parent.Id);
        parentEntity.Settle();
        await _db.SaveChangesAsync();
        var request = MakeIssueRequest(appointment.Id) with { ParentInvoiceId = parent.Id };

        Func<Task> act = () => _handler.IssueAsync(request);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Thu phần còn lại hợp lệ phải tạo hóa đơn con gắn đúng ParentInvoiceId với số tiền bằng phần còn lại.</summary>
    [Test]
    public async Task IssueAsync_RemainingCollection_ValidRequest_CreatesChildInvoiceWithRemainingAmount()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 200_000m));
        var request = MakeIssueRequest(appointment.Id) with { ParentInvoiceId = parent.Id };

        var result = await _handler.IssueAsync(request);

        result.ParentInvoiceId.Should().Be(parent.Id);
        result.TotalAmount.Should().Be(300_000m); // 500_000 - 200_000 đã đặt cọc
    }

    /// <summary>Đã có hóa đơn con thu phần còn lại rồi thì không cho tạo thêm hóa đơn con thứ hai.</summary>
    [Test]
    public async Task IssueAsync_RemainingCollection_AlreadyHasChildInvoice_ThrowsConflictException()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 200_000m));
        await _handler.IssueAsync(MakeIssueRequest(appointment.Id) with { ParentInvoiceId = parent.Id });

        Func<Task> act = () => _handler.IssueAsync(MakeIssueRequest(appointment.Id) with { ParentInvoiceId = parent.Id });

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

        var result = await _handler.GetOutstandingPlansAsync();

        result.Should().ContainSingle(p => p.TreatmentPlanId == inProgressPlan.Id);
        result[0].RemainingAmount.Should().Be(service.Price);
    }

    /// <summary>Xác nhận thanh toán cho hóa đơn thu phần còn lại phải tất toán (Settle) hóa đơn gốc.</summary>
    [Test]
    public async Task ConfirmPaymentAsync_RemainingCollectionInvoice_SettlesParentInvoice()
    {
        var (appointment, _, _) = await SeedPendingPaymentAppointmentAsync();
        var parent = await _handler.IssueAsync(MakeIssueRequest(appointment.Id, "deposit", deposit: 200_000m));
        var child = await _handler.IssueAsync(MakeIssueRequest(appointment.Id) with { ParentInvoiceId = parent.Id });

        await _handler.ConfirmPaymentAsync(child.Id, new ConfirmPaymentRequest(null));

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
        var invoice = await _handler.IssueAsync(MakeIssueRequest(appointment.Id) with { TreatmentPlanId = plan.Id });

        await _handler.ConfirmPaymentAsync(invoice.Id, new ConfirmPaymentRequest(null));

        (await _db.TreatmentPlans.SingleAsync(p => p.Id == plan.Id)).Status.Should().Be(TreatmentPlanStatus.Completed);
    }
}
