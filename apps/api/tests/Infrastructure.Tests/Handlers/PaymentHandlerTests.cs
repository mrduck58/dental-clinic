using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Application.UseCases.Payments;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using DentalClinic.API.Infrastructure.Persistence;
using DentalClinic.API.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Infrastructure.Tests.Handlers;

[TestFixture]
public class PaymentHandlerTests
{
    private AppDbContext _db = null!;
    private IPaymentGatewayResolver _gatewayResolver = null!;
    private IPaymentGatewayService _gatewayService = null!;
    private IPaymentConfirmationService _confirmationService = null!;
    private CreatePaymentRequestHandler _createRequestHandler = null!;
    private HandlePaymentWebhookHandler _webhookHandler = null!;
    private GetPaymentStatusHandler _getStatusHandler = null!;

    private static readonly IReadOnlyDictionary<string, string> EmptyHeaders = new Dictionary<string, string>();

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _gatewayResolver = Substitute.For<IPaymentGatewayResolver>();
        _gatewayService = Substitute.For<IPaymentGatewayService>();
        _gatewayResolver.Resolve(PaymentGateway.PayOS).Returns(_gatewayService);

        var notificationService = Substitute.For<INotificationService>();
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetUserIdsByRoleAsync("Staff", Arg.Any<CancellationToken>()).Returns(new List<Guid>());
        var invoiceQuery = new InvoiceQueryHelper(_db, new TreatmentPlanRepository(_db));
        _confirmationService = new PaymentConfirmationService(_db, notificationService, userRepo, invoiceQuery);

        var configuration = Substitute.For<IConfiguration>();
        _createRequestHandler = new CreatePaymentRequestHandler(_db, _gatewayResolver, configuration);
        _webhookHandler = new HandlePaymentWebhookHandler(
            _db, _gatewayResolver, _confirmationService, NullLogger<HandlePaymentWebhookHandler>.Instance);
        _getStatusHandler = new GetPaymentStatusHandler(
            _db, _gatewayResolver, _confirmationService, NullLogger<GetPaymentStatusHandler>.Instance);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Invoice> SeedUnpaidInvoiceAsync(decimal depositAmount = 500_000m)
    {
        var patientUser = User.Create("pay-p", $"pay-p-{Guid.NewGuid()}@test.com", "hash", UserRole.Patient);
        var dentistUser = User.Create("pay-d", $"pay-d-{Guid.NewGuid()}@test.com", "hash", UserRole.Dentist);
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

        var invoice = Invoice.Issue(
            appointment.Id, "INV001",
            new[] { ("Trám răng", 1, depositAmount, (Guid?)null, (decimal?)depositAmount) }, 0, PaymentMethod.OnlinePayment);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    /// <summary>Tạo yêu cầu thanh toán cho hóa đơn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task CreatePaymentRequestAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _createRequestHandler.Handle(
            new CreatePaymentRequestCommand(Guid.NewGuid(), PaymentGateway.PayOS), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Hóa đơn đã thanh toán rồi thì không cho tạo yêu cầu thanh toán mới.</summary>
    [Test]
    public async Task CreatePaymentRequestAsync_InvoiceAlreadyPaid_ThrowsConflictException()
    {
        var invoice = await SeedUnpaidInvoiceAsync();
        invoice.MarkAsPaid(PaymentMethod.Cash);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _createRequestHandler.Handle(
            new CreatePaymentRequestCommand(invoice.Id, PaymentGateway.PayOS), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    /// <summary>Đã có giao dịch Pending còn hiệu lực thì phải tái sử dụng, không tạo link mới.</summary>
    [Test]
    public async Task CreatePaymentRequestAsync_ExistingValidPendingTransaction_ReusesIt()
    {
        var invoice = await SeedUnpaidInvoiceAsync();
        var existing = PaymentTransaction.Create(
            invoice.Id, PaymentGateway.PayOS, "ORDER123", invoice.DepositAmount,
            "https://pay.url", null, null, DateTimeOffset.UtcNow.AddMinutes(10));
        _db.PaymentTransactions.Add(existing);
        await _db.SaveChangesAsync();

        var result = await _createRequestHandler.Handle(
            new CreatePaymentRequestCommand(invoice.Id, PaymentGateway.PayOS), CancellationToken.None);

        result.GatewayOrderCode.Should().Be("ORDER123");
        await _gatewayService.DidNotReceive().CreatePaymentLinkAsync(Arg.Any<CreatePaymentLinkRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Chưa có giao dịch nào phải gọi cổng thanh toán tạo link mới và lưu lại giao dịch.</summary>
    [Test]
    public async Task CreatePaymentRequestAsync_NoPendingTransaction_CreatesNewTransactionViaGateway()
    {
        var invoice = await SeedUnpaidInvoiceAsync();
        _gatewayService.CreatePaymentLinkAsync(Arg.Any<CreatePaymentLinkRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreatePaymentLinkResult("https://pay.url/new", "qr-code-data", "NEWORDER456", DateTimeOffset.UtcNow.AddMinutes(15), "{}"));

        var result = await _createRequestHandler.Handle(
            new CreatePaymentRequestCommand(invoice.Id, PaymentGateway.PayOS), CancellationToken.None);

        result.GatewayOrderCode.Should().Be("NEWORDER456");
        result.CheckoutUrl.Should().Be("https://pay.url/new");
        (await _db.PaymentTransactions.CountAsync()).Should().Be(1);
    }

    /// <summary>Chữ ký webhook không hợp lệ phải bị từ chối ngay, không xử lý giao dịch.</summary>
    [Test]
    public async Task HandleWebhookAsync_InvalidSignature_ThrowsValidationException()
    {
        _gatewayService.VerifyAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(false, false, "ANY", null, 0, "{}", null));

        Func<Task> act = () => _webhookHandler.Handle(
            new HandlePaymentWebhookCommand(PaymentGateway.PayOS, "{}", EmptyHeaders), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Webhook với mã đơn hàng không xác định (không khớp giao dịch nào) phải bị bỏ qua êm, không ném lỗi.</summary>
    [Test]
    public async Task HandleWebhookAsync_UnknownOrderCode_CompletesWithoutThrowing()
    {
        _gatewayService.VerifyAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(true, true, "UNKNOWN-CODE", "gw-tx-1", 500_000m, "{}", null));

        Func<Task> act = () => _webhookHandler.Handle(
            new HandlePaymentWebhookCommand(PaymentGateway.PayOS, "{}", EmptyHeaders), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    /// <summary>Giao dịch đã ở trạng thái cuối (webhook trùng lặp) phải được bỏ qua — không xử lý lại lần 2.</summary>
    [Test]
    public async Task HandleWebhookAsync_TransactionAlreadyTerminal_IsIdempotent()
    {
        var invoice = await SeedUnpaidInvoiceAsync();
        var tx = PaymentTransaction.Create(invoice.Id, PaymentGateway.PayOS, "ORDER-DONE", invoice.DepositAmount, null, null, null, null);
        tx.MarkSuccess("gw-1", "{}");
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();
        _gatewayService.VerifyAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(true, true, "ORDER-DONE", "gw-2", invoice.DepositAmount, "{}", null));

        await _webhookHandler.Handle(new HandlePaymentWebhookCommand(PaymentGateway.PayOS, "{}", EmptyHeaders), CancellationToken.None);

        (await _db.PaymentTransactions.SingleAsync(t => t.Id == tx.Id)).GatewayTransactionId.Should().Be("gw-1"); // không bị ghi đè
    }

    /// <summary>
    /// TC-Sy-03: Sau khi hóa đơn đặt cọc đã Paid, PayOS gửi lại (duplicate delivery) ĐÚNG payload webhook cũ —
    /// phải được bỏ qua êm, không xử lý lại lần 2, không ném lỗi, không ghi đè dữ liệu giao dịch gốc.
    /// </summary>
    [Test]
    public async Task HandleWebhookAsync_ReplaySamePayloadAfterInvoiceAlreadyPaid_IsIdempotent()
    {
        var invoice = await SeedUnpaidInvoiceAsync(depositAmount: 500_000m);
        var tx = PaymentTransaction.Create(invoice.Id, PaymentGateway.PayOS, "ORDER-REPLAY", 500_000m, null, null, null, null);
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();
        const string rawPayload = "{\"data\":{\"orderCode\":\"ORDER-REPLAY\"}}";
        _gatewayService.VerifyAndParseWebhookAsync(rawPayload, Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(true, true, "ORDER-REPLAY", "gw-original", 500_000m, rawPayload, null));

        // Webhook lần 1 (thật): đánh dấu Paid như bình thường.
        await _webhookHandler.Handle(new HandlePaymentWebhookCommand(PaymentGateway.PayOS, rawPayload, EmptyHeaders), CancellationToken.None);
        (await _db.Invoices.SingleAsync(i => i.Id == invoice.Id)).Status.Should().Be(PaymentStatus.Paid);

        // PayOS gửi lại đúng payload cũ (duplicate delivery) — không được throw, không xử lý lại.
        Func<Task> replay = () => _webhookHandler.Handle(
            new HandlePaymentWebhookCommand(PaymentGateway.PayOS, rawPayload, EmptyHeaders), CancellationToken.None);
        await replay.Should().NotThrowAsync();

        (await _db.Invoices.SingleAsync(i => i.Id == invoice.Id)).Status.Should().Be(PaymentStatus.Paid);
        var finalTxn = await _db.PaymentTransactions.SingleAsync(t => t.Id == tx.Id);
        finalTxn.Status.Should().Be(TransactionStatus.Success);
        finalTxn.GatewayTransactionId.Should().Be("gw-original"); // không bị xử lý/ghi đè lại ở lần webhook thứ 2
    }

    /// <summary>Số tiền webhook báo lệch quá nhiều so với giao dịch phải bị đánh dấu Failed, không tự động Paid.</summary>
    [Test]
    public async Task HandleWebhookAsync_AmountMismatch_MarksTransactionFailedNotSuccess()
    {
        var invoice = await SeedUnpaidInvoiceAsync(depositAmount: 500_000m);
        var tx = PaymentTransaction.Create(invoice.Id, PaymentGateway.PayOS, "ORDER-MISMATCH", 500_000m, null, null, null, null);
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();
        _gatewayService.VerifyAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(true, true, "ORDER-MISMATCH", "gw-1", 100_000m, "{}", null));

        await _webhookHandler.Handle(new HandlePaymentWebhookCommand(PaymentGateway.PayOS, "{}", EmptyHeaders), CancellationToken.None);

        (await _db.PaymentTransactions.SingleAsync(t => t.Id == tx.Id)).Status.Should().Be(TransactionStatus.Failed);
        (await _db.Invoices.SingleAsync(i => i.Id == invoice.Id)).Status.Should().Be(PaymentStatus.Unpaid);
    }

    /// <summary>Webhook báo thanh toán thành công hợp lệ phải đánh dấu giao dịch Success và xác nhận hóa đơn đã thanh toán.</summary>
    [Test]
    public async Task HandleWebhookAsync_SuccessfulPayment_MarksSuccessAndConfirmsInvoice()
    {
        var invoice = await SeedUnpaidInvoiceAsync(depositAmount: 500_000m);
        var tx = PaymentTransaction.Create(invoice.Id, PaymentGateway.PayOS, "ORDER-OK", 500_000m, null, null, null, null);
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();
        _gatewayService.VerifyAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(true, true, "ORDER-OK", "gw-tx-ok", 500_000m, "{}", null));

        await _webhookHandler.Handle(new HandlePaymentWebhookCommand(PaymentGateway.PayOS, "{}", EmptyHeaders), CancellationToken.None);

        (await _db.PaymentTransactions.SingleAsync(t => t.Id == tx.Id)).Status.Should().Be(TransactionStatus.Success);
        (await _db.Invoices.SingleAsync(i => i.Id == invoice.Id)).Status.Should().Be(PaymentStatus.Paid);
    }

    /// <summary>Webhook báo thất bại từ cổng thanh toán phải đánh dấu giao dịch Failed với lý do tương ứng.</summary>
    [Test]
    public async Task HandleWebhookAsync_GatewayReportsFailure_MarksTransactionFailed()
    {
        var invoice = await SeedUnpaidInvoiceAsync();
        var tx = PaymentTransaction.Create(invoice.Id, PaymentGateway.PayOS, "ORDER-FAIL", invoice.DepositAmount, null, null, null, null);
        _db.PaymentTransactions.Add(tx);
        await _db.SaveChangesAsync();
        _gatewayService.VerifyAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(true, false, "ORDER-FAIL", null, 0, "{}", "Người dùng hủy giao dịch"));

        await _webhookHandler.Handle(new HandlePaymentWebhookCommand(PaymentGateway.PayOS, "{}", EmptyHeaders), CancellationToken.None);

        var updated = await _db.PaymentTransactions.SingleAsync(t => t.Id == tx.Id);
        updated.Status.Should().Be(TransactionStatus.Failed);
        updated.FailureReason.Should().Be("Người dùng hủy giao dịch");
    }

    /// <summary>Tra cứu trạng thái cho hóa đơn không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task GetStatusAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _getStatusHandler.Handle(new GetPaymentStatusQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Hóa đơn chưa có giao dịch nào phải trả về LatestTransaction = null, không lỗi.</summary>
    [Test]
    public async Task GetStatusAsync_NoTransactionYet_ReturnsNullLatestTransaction()
    {
        var invoice = await SeedUnpaidInvoiceAsync();

        var result = await _getStatusHandler.Handle(new GetPaymentStatusQuery(invoice.Id), CancellationToken.None);

        result.LatestTransaction.Should().BeNull();
        result.InvoiceStatus.Should().Be(PaymentStatus.Unpaid.ToString());
    }

    /// <summary>Hóa đơn có nhiều giao dịch phải trả về đúng giao dịch MỚI NHẤT, không phải giao dịch đầu tiên.</summary>
    [Test]
    public async Task GetStatusAsync_MultipleTransactions_ReturnsLatestOne()
    {
        var invoice = await SeedUnpaidInvoiceAsync();
        var older = PaymentTransaction.Create(invoice.Id, PaymentGateway.PayOS, "ORDER-OLD", invoice.DepositAmount, null, null, null, null);
        _db.PaymentTransactions.Add(older);
        await _db.SaveChangesAsync();
        var newer = PaymentTransaction.Create(invoice.Id, PaymentGateway.PayOS, "ORDER-NEW", invoice.DepositAmount, null, null, null, null);
        _db.PaymentTransactions.Add(newer);
        await _db.SaveChangesAsync();

        var result = await _getStatusHandler.Handle(new GetPaymentStatusQuery(invoice.Id), CancellationToken.None);

        result.LatestTransaction.Should().NotBeNull();
        result.LatestTransaction!.GatewayOrderCode.Should().Be("ORDER-NEW");
    }
}
