using DentalClinic.API.Application.UseCases.Invoices;
using DentalClinic.API.Application.UseCases.Payments;
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
public class PaymentHandlerTests
{
    private AppDbContext _db = null!;
    private IPaymentGatewayResolver _gatewayResolver = null!;
    private IPaymentGatewayService _gatewayService = null!;
    private InvoiceHandler _invoiceHandler = null!;
    private PaymentHandler _handler = null!;

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
        _invoiceHandler = new InvoiceHandler(_db, notificationService, userRepo);

        _handler = new PaymentHandler(_db, _gatewayResolver, _invoiceHandler);
    }

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    private async Task<Invoice> SeedUnpaidInvoiceAsync(decimal depositAmount = 500_000m)
    {
        var patientUser = User.Create("pay-p", $"pay-p-{Guid.NewGuid()}@test.com", "hash", "Patient");
        var dentistUser = User.Create("pay-d", $"pay-d-{Guid.NewGuid()}@test.com", "hash", "Dentist");
        _db.Users.AddRange(patientUser, dentistUser);
        var patient = Patient.Create("Bệnh nhân Thanh Toán", new DateOnly(1990, 1, 1), "Nam", patientUser.Id);
        var dentist = Dentist.Create(dentistUser.Id, "BS Thanh Toán", "Nha khoa tổng quát", 5);
        _db.Patients.Add(patient);
        _db.Dentists.Add(dentist);
        var appointment = Appointment.Create(patient.Id, dentist.Id, DateTimeOffset.UtcNow);
        appointment.StartTreatment();
        appointment.EndTreatment();
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        var invoice = Invoice.Issue(
            appointment.Id, "INV001",
            new[] { ("Trám răng", 1, depositAmount) }, 0, PaymentMethod.OnlinePayment, PaymentType.Full, depositAmount);
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    /// <summary>Tạo yêu cầu thanh toán cho hóa đơn không tồn tại phải báo lỗi NotFoundException.</summary>
    [Test]
    public async Task CreatePaymentRequestAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.CreatePaymentRequestAsync(Guid.NewGuid(), PaymentGateway.PayOS);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Hóa đơn đã thanh toán rồi thì không cho tạo yêu cầu thanh toán mới.</summary>
    [Test]
    public async Task CreatePaymentRequestAsync_InvoiceAlreadyPaid_ThrowsConflictException()
    {
        var invoice = await SeedUnpaidInvoiceAsync();
        invoice.MarkAsPaid(PaymentMethod.Cash);
        await _db.SaveChangesAsync();

        Func<Task> act = () => _handler.CreatePaymentRequestAsync(invoice.Id, PaymentGateway.PayOS);

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

        var result = await _handler.CreatePaymentRequestAsync(invoice.Id, PaymentGateway.PayOS);

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

        var result = await _handler.CreatePaymentRequestAsync(invoice.Id, PaymentGateway.PayOS);

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

        Func<Task> act = () => _handler.HandleWebhookAsync(PaymentGateway.PayOS, "{}", EmptyHeaders);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>Webhook với mã đơn hàng không xác định (không khớp giao dịch nào) phải bị bỏ qua êm, không ném lỗi.</summary>
    [Test]
    public async Task HandleWebhookAsync_UnknownOrderCode_CompletesWithoutThrowing()
    {
        _gatewayService.VerifyAndParseWebhookAsync(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookVerificationResult(true, true, "UNKNOWN-CODE", "gw-tx-1", 500_000m, "{}", null));

        Func<Task> act = () => _handler.HandleWebhookAsync(PaymentGateway.PayOS, "{}", EmptyHeaders);

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

        await _handler.HandleWebhookAsync(PaymentGateway.PayOS, "{}", EmptyHeaders);

        (await _db.PaymentTransactions.SingleAsync(t => t.Id == tx.Id)).GatewayTransactionId.Should().Be("gw-1"); // không bị ghi đè
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

        await _handler.HandleWebhookAsync(PaymentGateway.PayOS, "{}", EmptyHeaders);

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

        await _handler.HandleWebhookAsync(PaymentGateway.PayOS, "{}", EmptyHeaders);

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

        await _handler.HandleWebhookAsync(PaymentGateway.PayOS, "{}", EmptyHeaders);

        var updated = await _db.PaymentTransactions.SingleAsync(t => t.Id == tx.Id);
        updated.Status.Should().Be(TransactionStatus.Failed);
        updated.FailureReason.Should().Be("Người dùng hủy giao dịch");
    }

    /// <summary>Tra cứu trạng thái cho hóa đơn không tồn tại phải báo lỗi.</summary>
    [Test]
    public async Task GetStatusAsync_InvoiceNotFound_ThrowsNotFoundException()
    {
        Func<Task> act = () => _handler.GetStatusAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>Hóa đơn chưa có giao dịch nào phải trả về LatestTransaction = null, không lỗi.</summary>
    [Test]
    public async Task GetStatusAsync_NoTransactionYet_ReturnsNullLatestTransaction()
    {
        var invoice = await SeedUnpaidInvoiceAsync();

        var result = await _handler.GetStatusAsync(invoice.Id);

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

        var result = await _handler.GetStatusAsync(invoice.Id);

        result.LatestTransaction.Should().NotBeNull();
        result.LatestTransaction!.GatewayOrderCode.Should().Be("ORDER-NEW");
    }
}
